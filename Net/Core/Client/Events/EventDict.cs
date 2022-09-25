using Net.Core.Messages;

namespace Net.Core.Client.Events;

public delegate void
    Event(INetMessage message);

public class EventDict
{
    private IDictionary<string, List<Event>> _events;

    public EventDict()
    {
        _events = new Dictionary<string, List<Event>>();
    }

    public void Add(string eventId, Event handler)
    {
        if (!_events.ContainsKey(eventId))
        {
            _events[eventId] = new List<Event>();
        }

        _events[eventId].Add(handler);
    }

    public List<Event>? EventsFor(string eventId)
    {
        if (!_events.ContainsKey(eventId))
            return null;
        return _events[eventId];
    }

    public Event? FirstEventFor(string eventId)
    {
        return EventsFor(eventId)?.FirstOrDefault();
    }
}