using Net.Core.Server.Connection.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Net.Core.Messages;

public interface INetMessage
{
    /// <summary>
    /// Short identifier that explains what message/event this is.
    /// Example: ('connected', 'disconnected')
    /// </summary>
    public string EventId { get; set; }

    /// <summary>
    /// Key-Value arguments sent with the net message.
    /// Example: If the EventId was 'unauthorized' (for example)
    /// the 'reason' could be sent with the properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; }

    /// <summary>
    /// Notifies the receiver if you want a response or not.
    /// </summary>
    public bool WantsResponse { get; set; }
}

/// <summary>
/// Serves as a base for INetMessage. This is the default type that
/// will be networked.
/// </summary>
public class NetMessage<T> : INetMessage where T : ICLIdentifier
{
    public NetMessage()
    {
        Properties = new();
        EventId = string.Empty;
        WantsResponse = false;
        Identity = default;
    }

    public NetMessage(
        Dictionary<string, object> properties, 
        string eventId, 
        bool wantsResponse,
        T? identity = default)
    {
        Properties = properties;
        EventId = eventId; 
        WantsResponse = wantsResponse;
        Identity = identity;
    }

    public Dictionary<string, object> Properties { get; set; } = null!;
    public string EventId { get; set; } = null!;
    public bool WantsResponse { get; set; } = false;
    public T? Identity { get; set; }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }

    // defaults //

    public static NetMessage<T> Connected
        => new NetMessageBuilder<T>()
            .WithEventId("connected")
            // .WantsResponse() << false
            // .WithClientIdentifier << None
            .Build();

    public static NetMessage<T> Connecting
        => new NetMessageBuilder<T>()
            .WithEventId("connecting")
            .WantsResponse()
            .Build();

    public static NetMessage<T> Rejected
        => new NetMessageBuilder<T>()
            .WithEventId("rejected")
            .WithProperty("reason", "no credentials supplied.")
            .Build();
}

public class NetMessageBuilder<T> where T : ICLIdentifier
{
    private Dictionary<string, object> Properties;
    private string EventId;
    private bool WantsRes;
    private T? clientIdentifier;

    public NetMessageBuilder()
    {
        EventId = string.Empty;
        Properties = new();
        WantsRes = false;
        clientIdentifier = default;
    }

    public NetMessageBuilder<T> WithProperty<U>(string Identifier, U instance) where U : notnull
    {
        Properties.Add(Identifier, instance);
        return this;
    }

    public NetMessageBuilder<T> WithEventId(string Identifier)
    {
        EventId = Identifier;
        return this;
    }

    public NetMessageBuilder<T> WantsResponse()
    {
        WantsRes = true;
        return this;
    }

    public NetMessageBuilder<T> WithClientIdentifier(T identifier)
    {
        clientIdentifier = identifier;
        return this;
    }

    public NetMessage<T> Build()
    {
        return new(Properties, EventId, WantsRes, clientIdentifier);
    }   
}
