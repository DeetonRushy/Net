using Microsoft.Extensions.Logging;
using Net.Config;
using Net.Console;
using Net.Core.Logging;
using Net.Core.Messages;
using Net.Core.ResourceParser;
using Net.Core.Server.Connection.Identity;
using Net.Extensions;
using System.Net;
using System.Net.Sockets;

namespace Net.Core.Server;

public class NetServer<CLIdentity> : INetworkInterface, IDisposable where CLIdentity : IClientIdentifier, new()
{
    private Socket? _socket;
    private List<Socket>? _rawConnections;
    private readonly List<CLIdentity> _connectedClients;
    private Thread? _acceptor;

    private readonly Logging.ILogger _logger;
    private readonly ConsoleSystem _console;

    private readonly bool _debugMode;

    private readonly object _lock = new();

    public NetServer(Logging.ILogger? logger = null)
    {
        _logger = logger
            ?? new DebugLogger("NetServer");
        _debugMode = ServerConfig.GetFlag("debug") != null;
        _console = new ConsoleSystem();
        _connectedClients = new();

        _console.AddCommand("broadcast.resource", (args) =>
        {
            if (args.Count != 1)
            {
                _console.WriteLine("usage: broadcast.resource <resource-s>");
                _console.WriteLine("example: broadcast.resource sendmessage?text=Hi");
                return;
            }

            ResourceConversionEngine<NetMessage<CLIdentity>> engine =
                new ();

            NetMessage<CLIdentity>? result;

            try
            {
                result = engine.Parse(args[0]);
            }
            catch (Exception ex)
            {
                _console.WriteLine($"failed to parse resource ({ex.Message})");
                return;
            }

            if (result is null)
            {
                _console.WriteLine("failed to parse resource string");
                return;
            }

            Task.Run(async () =>
            {
                await Broadcast(result);
            });
        });
        _console.AddCommand("server.clients", (_) =>
        {
            if (_connectedClients.Count == 0)
            {
                System.Console.WriteLine("There is nobody connected.");
                return;
            }

            foreach (var cl in _connectedClients)
            {
                System.Console.WriteLine($"{cl.Name} - {cl.Id} Connected: {cl.Socket?.Connected}");
            }
        });
    }

    ~NetServer()
    {
        Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _acceptor?.Join();

        Shutdown<NetMessage<DefaultId>>("Server is closing").RunSynchronously();

        _socket?.Dispose();
    }

    public async Task<INetMessage?> Send(Socket socket, INetMessage msg)
    {
        if (_rawConnections is null)
            return null;
        await socket.SendNetMessage(msg);
        var response = await WaitForMessage<NetMessage<CLIdentity>>(TimeSpan.FromSeconds(2));

        _logger?.Info($"Received message for event '{response?.Message?.EventId}'");

        return response?.Message;
    }

    public async Task<T?> Send<T>(Socket socket, T msg) where T: INetMessage
    {
        await socket.SendNetMessage(msg);
        return await socket.ReadNetMessage<T>();
    }

    public async Task RhetoricalSendTo<T>(Socket socket, T msg) where T : INetMessage
    {
        msg.WantsResponse = false;
        await socket.SendNetMessage(msg);
    }
    public async Task RhetoricalSendTo<T>(IdentityType identifierType, string identifier, T msg) where T : INetMessage
    {
        /* 
 * Identifiers in _connectedClients should always have their Socket
 * member set to a valid socket. So here, use the bang operator as they
 * should not be null. This will cause NullReferenceExceptions if there is
 * a bug. We want to know about bugs.
 */
        msg.WantsResponse = false;

        switch (identifierType)
        {
            case IdentityType.Name:
                {
                    if (!_connectedClients.Any(x => x.Name == identifier))
                    {
                        return;
                    }

                    var client = _connectedClients
                        .Where(x => x.Name == identifier)
                        .First();

                    await client!.Socket!.SendNetMessage(msg);

                    return;
                }
            case IdentityType.Id:
                {
                    if (!_connectedClients.Any(x => x.Id.ToString() == identifier))
                    {
                        return;
                    }

                    var client = _connectedClients
                        .Where(x => x.Id.ToString() == identifier)
                        .First();

                    await client!.Socket!.SendNetMessage(msg);

                    return;
                }
            default:
                throw new ArgumentOutOfRangeException($"IdentityType({identifierType}) is not implemented.");
        }
    }

    public async Task<INetMessage?> SendTo(IdentityType identifierType, string identifier, INetMessage message)
    {
        /* 
         * Identifiers in _connectedClients should always have their Socket
         * member set to a valid socket. So here, use the bang operator as they
         * should not be null. This will cause NullReferenceExceptions if there is
         * a bug. We want to know about bugs.
         */

        switch (identifierType)
        {
            case IdentityType.Name:
                {
                    if (!_connectedClients.Any(x => x.Name == identifier))
                    {
                        return (INetMessage?)default(DefaultId);
                    }

                    var client = _connectedClients
                        .Where(x => x.Name == identifier)
                        .First();

                    return await Send(client.Socket!, message);
                }
            case IdentityType.Id:
                {
                    if (!_connectedClients.Any(x => x.Id.ToString() == identifier))
                    {
                        return (INetMessage?)default(DefaultId);
                    }

                    var client = _connectedClients
                        .Where(x => x.Id.ToString() == identifier)
                        .First();

                    return await Send(client.Socket!, message);
                }
            default:
                throw new ArgumentOutOfRangeException($"IdentityType({identifierType}) is not implemented.");
        }
    }
    public async Task<T?> SendTo<T>(IdentityType identifierType, string identifier, INetMessage message) where T: INetMessage
    {
        return (T?)await SendTo(identifierType, identifier, message);
    }

    public async Task<bool> Start(string ip, int port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var localAddress = Dns.GetHostEntry(ip).AddressList[1];
        var endpoint = new IPEndPoint(localAddress, port);
        _socket.Bind(endpoint);
        _socket.Listen();

        await _logger.InfoAsync(_console, $"Server started on ({ip}:{port}) [{(_debugMode ? "Debug" : "Release")}]");

        _acceptor = new(ServerPacketAcceptor)
        { 
            Name = "Net.Server.SocketListener" 
        };

        _acceptor.Start();
        _rawConnections = new();
        _console.MainLoop();

        return _socket.Connected;
    }

    public async Task<MessageInfo?> WaitForMessage<T>() where T : INetMessage
    {
        return await WaitForMessage<T>(TimeSpan.MaxValue);
    }

    public async Task<MessageInfo?> WaitForMessage<T>(TimeSpan timeout) where T : INetMessage
    {
        SpinWait.SpinUntil(() =>
        {
            if (_rawConnections is null)
                throw new NullReferenceException("connection list is null (did you start the server before waiting for messages?)");
            return _rawConnections.Any(x =>
            {
                return x.Available > 0;
            });
        }, timeout);

        var client = _rawConnections?.Where(x => x.Available > 0).FirstOrDefault();

        if (client is null)
        {
            return default;
        }

        var message = await client.ReadNetMessage<T>();

        if (message is null)
        {
            throw new InvalidDataException("Failed to read net message from client");
        }

        return new MessageInfo { Message = message, Sender = client };
    }

    public async Task TriggerEvent(INetMessage message)
    {
        foreach (var cl in _connectedClients)
        {
            await RhetoricalSendTo(IdentityType.Name, cl.Name, message);
        }
    }
    public async Task TriggerEventFor(IdentityType type, string identifier, INetMessage message)
    {
        await RhetoricalSendTo(type, identifier, message);
    }

    public async Task<T?> TriggerEventFor<T>(IdentityType type, string identifier, INetMessage message) where T : INetMessage
    {
        return await SendTo<T>(type, identifier, message);
    }

    public async Task Broadcast(INetMessage message)
    {
        if (_rawConnections is null)
        {
            await _logger.ErrorAsync(_console, "Cannot broadcast when the server hasn't been started.");
            return;
        }

        if (message.WantsResponse)
        {
            await _logger.WarnAsync(_console, $"Cannot fetch {_rawConnections.Count} responses from a broadcast. (WantsResponse is set on a broadcast)");
        }

        foreach (var client in _rawConnections)
        {
            await Send(client, message);
        }
    }
    public async Task Broadcast<T>(T message) where T: INetMessage
    {
        if (_rawConnections is null)
        {
            await _logger.ErrorAsync(_console, "Cannot broadcast when the server hasn't been started.");
            return;
        }

        if (message.WantsResponse)
        {
            await _logger.WarnAsync(_console, $"Cannot fetch {_rawConnections.Count} responses from a broadcast. (WantsResponse is set on a broadcast)");
        }

        foreach (var client in _rawConnections)
        {
            await Send(client, message);
        }
    }

    public async Task Shutdown<T>(string Reason) where T: class, INetMessage, new()
    {
        await Task.Run(async () =>
        {
            var resource = await Factory.MessageFromResourceString<T>($"shutdown?reason='{Reason}'");

            if (resource is not null)
            {
                // would only ever fail if there was a bug
                await Broadcast(resource);
                return;
            }

            _socket?.Shutdown(SocketShutdown.Both);

            _connectedClients?.ForEach((x) =>
            {
                x.Socket?.Close();
                x.Socket?.Dispose();
            });
        });
    }

    public void ServerPacketAcceptor()
    {
        while (true)
        {
            var sock = _socket.Accept();

            _ = Task.Run(async () =>
            {
                /* maybe do something with the response */
                var response = await
                    Send(sock, NetMessage<CLIdentity>.Connecting);

                if (response is null)
                {
                    await RhetoricalSendTo(sock, NetMessage<CLIdentity>.Rejected);
                    return;
                }

                if (response.Identity is null)
                {
                    await RhetoricalSendTo(sock, NetMessage<CLIdentity>.Rejected);
                    return;
                }

                if (ServerConfig.GetFlag("allowMultipleSessions") is null)
                {
                    if (_connectedClients.Any(x => x.Id == response.Identity.Id))
                    {
                        var rejection = new NetMessageBuilder<CLIdentity>()
                            .WithEventId("disallowed")
                            .WithProperty("reason", "Multiple sessions for the same user is disallowed")
                            .Build();
                        await RhetoricalSendTo(sock, rejection);
                        sock.Close();
                        return;
                    }
                }

                lock (_lock)
                {
                    var serverClient = new CLIdentity
                    {
                        Name = response.Identity.Name,
                        Socket = sock,
                        Id = response.Identity.Id
                    };

                    _logger?.Info($"Accepted client '{response.Identity.Name}'");
                    _rawConnections?.Add(sock);
                    _connectedClients?.Add(serverClient);
                }

                await RhetoricalSendTo(sock, NetMessage<CLIdentity>.Connected);
            });
        }
    }
}
