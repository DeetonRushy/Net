using Net.Core.Logging;
using Newtonsoft.Json;

namespace Net.Config;

public enum ConfigTarget
{
    Server,
    Client,
    Global
}

public class ConfigFlag
{
    public ConfigFlag(string identifier, List<string>? options = null)
    {
        Identifier = identifier;
        Options = options ?? new List<string>();
    }

    public void AddOption(string Option)
        => Options.Add(Option);

    public string Identifier { get; set; }
    public List<string> Options { get; set; }
}

public class ConfigurationManager
{
    private static ILogger? logger;

#if DEBUG
    static ConfigurationManager()
    {
        logger = new DebugLogger("Config");
    }
#endif

    public static void UseLogger<T>() where T : notnull, ILogger, new()
    {
        logger = new T();
    }

    public static void SetFlag(ConfigTarget target, ConfigFlag flag)
    {
        logger?.Info($"Loading flag for {target} ('{flag.Identifier}', {flag.Options.Count} options attached)");

        if (ServerConfig.GetFlag("dumpCfgOnWrite") is ConfigFlag)
        {
            logger?.Info($"(dumpCfgOnWrite): {JsonConvert.SerializeObject(ServerConfig.Flags)}");
        }

        switch (target)
        {
            case ConfigTarget.Server:
                ServerConfig.Flags.Add(flag);
                break;
            case ConfigTarget.Client:
                ClientConfig.Flags.Add(flag);
                break;
            case ConfigTarget.Global:
                ClientConfig.Flags.Add(flag);
                ServerConfig.Flags.Add(flag);
                break;
            default:
                throw new ArgumentOutOfRangeException($"ConfigTarget({target}): is not implemented.");
        }
    }

    public static void SetFlags(ConfigTarget target, params ConfigFlag[] flags)
    {
        foreach(var flag in flags)
            SetFlag(target, flag);
    }

    public static void RemoveFlag(ConfigTarget target, string FlagName)
    {
        logger?.Info($"Attempting unload of flag. ({target}, '{FlagName}')");

        switch (target)
        {
            case ConfigTarget.Client:
                {
                    ClientConfig.Flags.RemoveAll(x => x.Identifier == FlagName);
                    return;
                }
            case ConfigTarget.Server:
                {
                    ServerConfig.Flags.RemoveAll(x => x.Identifier == FlagName);
                    return;
                }
            case ConfigTarget.Global:
                {
                    ClientConfig.Flags.RemoveAll(x => x.Identifier == FlagName);
                    ServerConfig.Flags.RemoveAll(x => x.Identifier == FlagName);
                    return;
                }
            default:
                throw new ArgumentOutOfRangeException($"ConfigTarget({target}): is not implemented.");
        }
    }
}