using System.Net.Sockets;

namespace Net.Core.Messages;

public class MessageInfo
{
    public Socket? Sender { get; init; } = null!;
    public INetMessage? Message { get; init; } = null!;
}