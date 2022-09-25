namespace Net.Console;

public delegate void ConsoleCommand(List<string> args);

/// <summary>
/// Base template for this. It could (and should) become much more complex
/// (Hence why it has its own directory :D)
/// </summary>
public class ConsoleSystem
{
    private readonly IDictionary<string, ConsoleCommand> commands = new Dictionary<string, ConsoleCommand>();
    private readonly Thread reader;
    private readonly Thread uiManager;

    private static int GetThreadInfo()
    {
        ThreadPool.GetMaxThreads(out int maxThreads, out _);
        ThreadPool.GetAvailableThreads(out int availableThreads, out _);

        return maxThreads - availableThreads;
    }

    public void AddCommand(string Name, ConsoleCommand command)
    {
        commands.TryAdd(Name, command);
    }

    public ConsoleSystem()
    {
        commands.Add("commands", (args) =>
        {
            foreach (var command in commands.Keys)
            {
                WriteLine($"[Command]: {command}");
            }
        });

        reader = new Thread(() =>
        {
            WriteLine("Booted!");

            while (true)
            {
                System.Console.Write(">>> ");
                var text = System.Console.ReadLine();
                System.Console.WriteLine();
                if (text is null)
                    continue;
                var split = text?.Split(' ');
                if (split?.Length <= 0)
                    continue;
                var command = split![0];
                if (command == string.Empty)
                {
                    continue;
                }
                if (split?.Length == 1)
                {
                    if (commands.ContainsKey(command))
                    {
                        commands[command]
                            .Invoke(Array.Empty<string>().ToList());
                        continue;
                    }

                    WriteLine($"no command '{command}'");
                    continue;
                }
                else
                {
                    if (commands.ContainsKey(command))
                    {
                        commands[command]
                            .Invoke(split![1..].ToList());
                        continue;
                    }

                    WriteLine($"no command '{command}'");
                    continue;
                }
            }
        })
        { Name = "Net.Console.Reader" };
        uiManager = new Thread(() =>
        {
            while (true)
            {
                System.Console.Title = $"Task Count: {GetThreadInfo()}";
                Thread.Sleep(TimeSpan.FromMilliseconds(10));
            }
        })
        { Name = "Net.Ui.Manager (Console)" };
    }

    public void WriteLine(string message)
    {
        System.Console.WriteLine($"\n[Console] {message}");
    }

    public async Task WriteLineAsync(string message)
    {
        await System.Console.Out.WriteAsync($"\n[Console] {message}");   
    }

    /// <summary>
    /// Fires up a thread to read input from the console, then processes the text.
    /// </summary>
    public void MainLoop()
    {
        reader?.Start();
        uiManager?.Start();
    }
}