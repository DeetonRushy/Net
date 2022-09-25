using Net.Console;
using Pastel;
using System.Drawing;

namespace Net.Core.Logging;

public interface ILogger
{
    public string Target { get; }

    public void Info(string message);
    public Task InfoAsync(ConsoleSystem console, string message);

    public void Error(string message);
    public Task ErrorAsync(ConsoleSystem console, string message);

    public void Warn(string message);
    public Task WarnAsync(ConsoleSystem console, string message);
}

public class DebugLogger : ILogger
{
    // Padding

    // Broken, pls fix
    // This is supposed to make every DebugLogger instance write with the same padding to make it look nice

    private static List<string> GlobalTargets = new() { "unknown" };
    private static int Max
        => GlobalTargets?.Max()?.Length ?? 1;

    // Padding

    public DebugLogger()
    {
        Target = "unknown";
    }

    public DebugLogger(string target)
    {
        Target = target;
        GlobalTargets.Add(target);
    }

    public string Target { get; set; }

    public void Error(string message)
    {
#if DEBUG
        System.Console.WriteLine($"({Target}) {"".PadRight((Max - (Max - Target.Length)))}[{"Error".Pastel(Color.Red)}] {message}");
#endif
    }

    public async Task ErrorAsync(ConsoleSystem console, string message)
    {
#if DEBUG
        await console
            .WriteLineAsync
            (
            $"({Target}) {"".PadRight((Max - (Max - Target.Length)))}[{"Error".Pastel(Color.Red)}] {message}"
            );
#endif
    }

    public void Info(string message)
    {
        System.Console.WriteLine($"({Target}) {"".PadRight((Max - (Max - Target.Length)))}[{"Info".Pastel(Color.Cyan)}] {message}");
    }

    public async Task InfoAsync(ConsoleSystem console, string message)
    {
        await console
            .WriteLineAsync
            (
            $"({Target}) {"".PadRight((Max - (Max - Target.Length)))}[{"Info".Pastel(Color.Red)}] {message}"
            );
    }

    public void Warn(string message)
    {
        System.Console.WriteLine($"({Target}) {"".PadRight((Max - (Max - Target.Length)))} [{"Warn".Pastel(Color.OrangeRed)}] {message}");
    }

    public async Task WarnAsync(ConsoleSystem console, string message)
    {
        await console
            .WriteLineAsync
            (
            $"({Target}) {"".PadRight((Max - (Max - Target.Length)))}[{"Warn".Pastel(Color.Red)}] {message}"
            );
    }
}
