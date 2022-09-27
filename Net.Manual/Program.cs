
using Net.Manual;
using System.Reflection;

List<IManualResponder> responders = new List<IManualResponder>();

foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
{
    if (!t.IsClass || t == typeof(IManualResponder))
    {
        continue;
    }

    if (t.IsAssignableTo(typeof(IManualResponder)))
    {
        try
        {
            responders.Add((IManualResponder)Activator.CreateInstance(t)!);
        }
        catch
        {
            // no default constructor
        }
    }
}

if (args.Length <= 0)
{
    Console.WriteLine("Usage: Net.Manual <component-name>");
    return;
}

var component = args[0];
var responder = responders.Where(x => x.Name == component).FirstOrDefault();

if (responder is null)
{
    Console.WriteLine($"No component named '{component}'");
    Console.WriteLine("Available components:\n");

    foreach (var r in responders)
    {
        Console.WriteLine(r.Name);
    }
    return;
}

responder.Output();
