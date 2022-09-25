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
    private Socket _socket;
    private EventDict _events;

    private Thread _listener;

    ILogger? logger;

    private Identity? _localId;
    public void SetLocalIdentifier(Identity identifier)
    {
        _localId = identifier;
    }

    public void UseLogger<L>() where L : ILogger, new()
    {
        logger = new L();
    }

    public NetClient()
    {
        _events = new EventDict();
        _events.Add("connected", (message) =>
        {
            System.Console.WriteLine("Connected to server");
        });
        _events.Add("display", (message) =>
        {
            if (!message.Properties.ContainsKey("text"))
            {
                // bad params
                return;
            }

            System.Console.WriteLine(message.Properties["text"]);
        });

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.ReceiveTimeout = _socket.ReceiveTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;

        _listener = new(() =>
        {
            while (true)
            {
                SpinWait.SpinUntil(() => _socket.Available > 0);
                var packet = _socket.ReadNetMessage<Packet>().Result;
                
                if (packet is null)
                {
                    logger?.Warn("Received packet, but the format was not expected.");
                    continue;
                }

                FireEvent(packet.EventId, packet);
            }
        })
        { Name = "Net.Client.SocketListener" };

        // configuration purposes
        if (ClientConfig.GetFlag("socketTimeout") is ConfigFlag timeoutFlag)
        {
            if (timeoutFlag.Options.Count == 1)
            {
                if (!int.TryParse(timeoutFlag.Options.First(), out int timeOut))
                {
                    logger?.Warn("socketTimeout is set in the configuration, but its value is invalid.");
                }
                else
                {
                    logger?.Info($"socketTimeout set to '{timeOut}'");
                    _socket.ReceiveTimeout = timeOut;
                }
            }
        }
    }

    public async Task<INetMessage?> Send(Socket sock, INetMessage msg)
    {
        await sock.SendNetMessage(msg);
        return await sock.ReadNetMessage<Packet>();
    }

    public async Task<bool> Start(string ip, int port)
    {
        logger?.Info($"Client start on '{ip}:{port}'");

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
            return false;
#endif
        }

        if (response?.EventId == "rejected")
        {
#if DEBUG
            throw new Exception("server rejected the connection");
#else
            return false;
#endif
        }

        FireEvent(response?.EventId, response!);

        _listener.Start();

        return true;
    }

    public async Task<MessageInfo?> WaitForMessage<T>() where T : INetMessage
    {
        logger?.Info("Waiting for response");

        return new MessageInfo 
        { 
            Message = await _socket.ReadNetMessage<Packet>(),
            Sender = _socket
        };
    }

    public async Task<MessageInfo?> WaitForMessage<T>(TimeSpan timeout) where T : INetMessage
    {
        SpinWait.SpinUntil(() => _socket.Available > 0, timeout);
        return new MessageInfo
        {
            Message = await _socket.ReadNetMessage<T>(),
            Sender = _socket
        };
    }
    
    public void On(string Event, Event @event)
    {
        _events.Add(Event, @event);
    }

    private void FireEvent(string? id, INetMessage message)
    {
        logger?.Info($"event '{id}' fired");

        if (id is null)
            return;

        _events?.EventsFor(message.EventId)?
            .ForEach(x => x.Invoke(message));
    }
}