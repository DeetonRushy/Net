using Microsoft.CodeAnalysis.CSharp.Syntax;
using Net.Config;
using Net.Core.Client.Events;
using Net.Core.Logging;
using Net.Core.Messages;
using Net.Core.Server.Connection.Identity;
using Net.Extensions;
using System.Net;
using System.Net.Sockets;

namespace Net.Core.Client;

public class NetClient<Packet, Identity> 
    : INetworkInterface where Identity : IClientIdentifier 
    where Packet : INetMessage
{
    private readonly Socket _socket;
    private readonly EventDict _events;

    private readonly Thread _socketListener;

    ILogger? _logger;

    private Identity? _localId;

    /// <summary>
    /// Sets the identity that will be used to connect to the server.
    /// </summary>
    /// <param name="identifier"></param>
    public void SetLocalIdentifier(Identity identifier)
    {
        _localId = identifier;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ILogger"/> from <typeparamref name="L"/> and use it to log.
    /// </summary>
    /// <typeparam name="L">The logger to use</typeparam>
    public void UseLogger<L>() where L : ILogger, new()
    {
        _logger = new L();
    }

    public NetClient()
    {
        _events = new EventDict();
        _events.Add("display", (message) =>
        {
            if (!message.Properties.ContainsKey("text"))
            {
                // bad params
                return;
            }

            System.Console.WriteLine(message.Properties["text"]);
        });
        _events.Add("shutdown", (_) =>
        {
            _socket?.Close();
            _socket?.Dispose();
        });

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            ReceiveTimeout =
                (int)TimeSpan.FromSeconds(5).TotalMilliseconds
        };

        _socketListener = new(ListenForPacket)
        {
            /*for debugging purposes*/
            Name = "Net.Client.SocketListener" 
        };

        // configuration purposes
        if (ClientConfig.GetFlag("socketTimeout") is ConfigFlag timeoutFlag)
        {
            if (timeoutFlag.Options.Count == 1)
            {
                if (!int.TryParse(timeoutFlag.Options.First(), out int timeOut))
                {
                    _logger?.Warn("socketTimeout is set in the configuration, but its value is invalid.");
                }
                else
                {
                    _logger?.Info($"socketTimeout set to '{timeOut}'");
                    _socket.ReceiveTimeout = timeOut;
                }
            }
        }
    }

    /// <summary>
    /// Send an <see cref="INetMessage"/> to <paramref name="sock"/> and wait for a response.
    /// </summary>
    /// <param name="sock">The socket to send the data to</param>
    /// <param name="msg">The message to send</param>
    /// <returns>The response</returns>
    public async Task<INetMessage?> Send(Socket sock, INetMessage msg)
    {
        if (!msg.WantsResponse)
        {
            _logger?.Warn("You are using Client.Send but do not want a response. It is better practice to use RhetoricalSend instead.");
        }

        await sock.SendNetMessage(msg);
        return await sock.ReadNetMessage<Packet>();
    }

    /// <summary>
    /// Send an <see cref="INetMessage"/> to <paramref name="sock"/> without trying to get a response
    /// </summary>
    /// <param name="sock">The socket to send the data to</param>
    /// <param name="msg">The data</param>
    /// <returns></returns>
    public async Task RhetoricalSend(Socket sock, INetMessage msg)
    {
        await sock.SendNetMessage(msg);
    }

    /// <summary>
    /// Start the client. This will attempt to connect to the server. If we fail to connect to the server, the function will return false.
    /// If in debug mode, on failed connected an exception will be thrown. Once connected, the local identity will be communicated
    /// and the connection will be established. To call this function, you MUST have the local identifier set. This can be set through
    /// factory functions, or with <see cref="SetLocalIdentifier(Identity)"/>
    /// (Events: 'connecting', 'connected', 'rejected')
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="NullReferenceException"></exception>
    public async Task<bool> Start(string ip, int port)
    {
        _logger?.Info($"Client start on '{ip}:{port}'");

        var host = Dns.GetHostEntry(ip);
        var address = host.AddressList[1];
        var endpoint = new IPEndPoint(address, port);

        try
        {
            _socket.Connect(endpoint);
        }
        catch
        {
#if DEBUG
            throw;
#else
            return false;
#endif
        }

        var initial = await WaitForMessage<NetMessage<DefaultId>>();

        if (initial is null)
        {
            throw new Exception("server failed to send initial packet");
        }

        FireEvent(initial.Message?.EventId, initial.Message!);

        INetMessage? response = null;

        if (initial?.Message?.WantsResponse == true)
        {
            response = await Send(_socket,
            new NetMessageBuilder<Identity>()
                .WithClientIdentifier((_localId) ?? throw new NullReferenceException("Local Identifier must be assigned to."))
                .Build());
        }

        if (response is null)
        {
#if DEBUG
            throw new Exception("server failed to respond to the connection");
#else
            FireEvent(response?.EventId, response!);
            return false;
#endif
        }

        if (response?.EventId == "rejected")
        {
#if DEBUG
            throw new Exception("server rejected the connection");
#else
            FireEvent(response?.EventId, response!);
            return false;
#endif
        }

        FireEvent(response?.EventId, response!);

        _socketListener.Start();

        return true;
    }

    /// <summary>
    /// Waits for a response from the server.
    /// </summary>
    /// <typeparam name="T">The type to convert the message into</typeparam>
    /// <returns><see cref="MessageInfo"/></returns>
    public async Task<MessageInfo?> WaitForMessage<T>() where T : INetMessage
    {
        _logger?.Info("Waiting for response");

        return new MessageInfo 
        { 
            Message = await _socket.ReadNetMessage<Packet>(),
            Sender = _socket
        };
    }

    /// <summary>
    /// Waits for a response from the server, until the time waiting is bigger than or equal to <paramref name="timeout"/>.
    /// </summary>
    /// <typeparam name="T">The type to convert the message into</typeparam>
    /// <returns><see cref="MessageInfo"/></returns>
    public async Task<MessageInfo?> WaitForMessage<T>(TimeSpan timeout) where T : INetMessage
    {
        SpinWait.SpinUntil(() => _socket.Available > 0, timeout);
        return new MessageInfo
        {
            Message = await _socket.ReadNetMessage<T>(),
            Sender = _socket
        };
    }
    
    /// <summary>
    /// Register an event. This will execute when the server sends data with the eventId equal to <paramref name="Event"/>.
    /// (There can be multiple for the same event)
    /// </summary>
    /// <param name="Event">The event identifier. Eg: 'connected'</param>
    /// <param name="event">The callback to execute.</param>
    public void On(string Event, Event @event)
    {
        _events.Add(Event, @event);
    }

    private void FireEvent(string? id, INetMessage message)
    {
        _logger?.Info($"event '{id}' fired");

        if (id is null)
            return;

        _events?.EventsFor(message.EventId)?
            .ForEach(x => x.Invoke(message));
    }

    private void ListenForPacket()
    {
        while (true)
        {
            SpinWait.SpinUntil(() => _socket.Available > 0);

            if (_socket is null)
            {
                // server has shutdown
                break;
            }

            var packet = _socket.ReadNetMessage<Packet>().Result;

            if (packet is null)
            {
                _logger?.Warn("Received packet, but the format was not expected.");
                continue;
            }

            FireEvent(packet.EventId, packet);
        }
    }
}