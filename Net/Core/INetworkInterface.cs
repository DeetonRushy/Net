
using Net.Core.Messages;
using Net.Core.Server.Connection.Identity;
using System.Net.Sockets;

namespace Net.Core;

/// <summary>
/// Represents an object used for networking data.
/// </summary>
public interface INetworkInterface<I> where I: ICLIdentifier
{
    public Task<bool> Start(string ip, int port);
    public Task<INetMessage<I>?> Send(Socket sock, INetMessage<I> msg);
    public Task<MessageInfo<I>?> WaitForMessage<T>() where T : INetMessage<I>;
    public Task<MessageInfo<I>?> WaitForMessage<T>(TimeSpan timeout) where T : INetMessage<I>;
}
