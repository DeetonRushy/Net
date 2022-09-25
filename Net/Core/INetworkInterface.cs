
using Net.Core.Messages;
using System.Net.Sockets;

namespace Net.Core;

/// <summary>
/// Represents an object used for networking data.
/// </summary>
public interface INetworkInterface
{
    public Task<bool> Start(string ip, int port);
    public Task<INetMessage?> Send(Socket sock, INetMessage msg);
    public Task<MessageInfo?> WaitForMessage<T>() where T : INetMessage;
    public Task<MessageInfo?> WaitForMessage<T>(TimeSpan timeout) where T : INetMessage;
}
