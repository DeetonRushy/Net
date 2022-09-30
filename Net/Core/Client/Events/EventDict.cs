using Net.Core.Messages;
using Net.Core.Server.Connection.Identity;

namespace Net.Core.Client.Events;

public delegate void
    Event<T>(INetMessage<T> message) where T: ICLIdentifier;

public class EventDict<Identity> where Identity: ICLIdentifier
{
    private IDictionary<string, List<Event<Identity>>> _events;

    public EventDict()
    {
        _events = new Dictionary<string, List<Event<Identity>>>();
    }

    public void Add(string eventId, Event<Identity> handler)
    {
        if (!_events.ContainsKey(eventId))
        {
            _events[eventId] = new List<Event<Identity>>();
        }

        _events[eventId].Add(handler);
    }

    public List<Event<Identity>>? EventsFor(string eventId)
    {
        if (!_events.ContainsKey(eventId))
            return null;
        return _events[eventId];
    }

    public Event<Identity>? FirstEventFor(string eventId)
    {
        return EventsFor(eventId)?.FirstOrDefault();
    }
}