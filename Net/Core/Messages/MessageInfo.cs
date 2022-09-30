using Net.Core.Server.Connection.Identity;
using System.Net.Sockets;

namespace Net.Core.Messages;

public class MessageInfo<I> where I: ICLIdentifier
{
    public Socket? Sender { get; init; } = null!;
    public INetMessage<I>? Message { get; init; } = null!;
}