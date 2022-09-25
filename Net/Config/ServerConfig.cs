namespace Net.Config;

internal class ServerConfig
{
    public static List<ConfigFlag> Flags { get; set; } = new List<ConfigFlag>();

    public static ConfigFlag? GetFlag(string Name)
    {
        return Flags
            .FirstOrDefault(x => x?.Identifier == Name, null);
    }
}