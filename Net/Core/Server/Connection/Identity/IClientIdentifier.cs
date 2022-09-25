using System.Net.Sockets;

namespace Net.Core.Server.Connection.Identity;

public interface ICLIdentifier
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public Socket? Socket { get; init; }
}