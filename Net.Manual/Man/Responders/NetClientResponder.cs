using Pastel;
using System.Drawing;

namespace Net.Manual.Responders;

public class NetClientResponder : IManualResponder
{
    public string Name => "NetClient";
    public string OutputString = $@"
Main server component

public class {"NetServer".Pastel(Color.Green)}<CLIdentity> : INetworkInterface, IDisposable where CLIdentity : ICLIdentifier, new()

Type parameters: 
    {"CLIdentity".Pastel(Color.YellowGreen)}:
        The class that implements CLIdentifier. This will be used to communicate
        identitys.

This class provides the absolute base functionality of Net's server application.

It provides means of Sending, Receiving and processing data.
";

    public void Output()
    {
        Console.WriteLine(OutputString);
    }
}