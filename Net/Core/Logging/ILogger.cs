using Net.Config;
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


// FIXME: absolute fucking mess
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
#if DEBUG
        ConfigurationManager.SetFlag(ConfigTarget.Server, new ConfigFlag("logPath", new() { "output.log" }));
#endif

        // only log on the server
        if (ServerConfig.GetFlag("logPath") is ConfigFlag flag)
        {
            if (flag.Options.Count != 1)
            {
                throw new Exception("The option logPath requires one option. (the path)");
            }

            var file = flag.Options[0];
            
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        Target = target;
        GlobalTargets.Add(target);
    }

    private string BuildMsg(Color color, string type, string t)
    {
        return $"({Target}) {"".PadRight(Max - (Max - Target.Length))}[{type.Pastel(color)}] {t}";
    }
    private string BuildFMsg(string type, string t)
    {
        return $"({Target}) {"".PadRight(Max - (Max - Target.Length))}[{type}] {t}";
    }
    private void WriteToLog(string type, string t)
    {
        if (ServerConfig.GetFlag("logPath") is ConfigFlag flag)
        {
            File.AppendAllText(flag.Options[0], BuildFMsg(type, t) + "\n");
        }
    }

    private async Task WriteToLogAsync(string type, string t)
    {
        if (ServerConfig.GetFlag("logPath") is ConfigFlag flag)
        {
            await File.AppendAllTextAsync(flag.Options[0], BuildFMsg(type, t) + "\n");
        }
    }

    public string Target { get; set; }

    public void Error(string message)
    {
        System.Console.WriteLine(BuildMsg(Color.Red, "Error", message));
        
    }

    public async Task ErrorAsync(ConsoleSystem console, string message)
    {
        var built = BuildMsg(Color.Red, "Error", message);

        await console
            .WriteLineAsync
            (
            built
            );
        await WriteToLogAsync("Error", message);
    }

    public void Info(string message)
    {
        var built = BuildMsg(Color.Blue, "Info", message);
        System.Console.WriteLine(built);
        WriteToLog("Info", message);
    }

    public async Task InfoAsync(ConsoleSystem console, string message)
    {
        var built = BuildMsg(Color.Blue, "Info", message);

        await console
            .WriteLineAsync
            (
            built
            );
        await WriteToLogAsync("Info", message);
    }

    public void Warn(string message)
    {
        var built = BuildMsg(Color.OrangeRed, "Warning", message);
        System.Console.WriteLine(built);

        WriteToLog("Warning", message);
    }

    public async Task WarnAsync(ConsoleSystem console, string message)
    {
        var built = BuildMsg(Color.OrangeRed, "Warning", message);

        await console
            .WriteLineAsync
            (
            built
            );
        await WriteToLogAsync("Warning", message);
    }
}
