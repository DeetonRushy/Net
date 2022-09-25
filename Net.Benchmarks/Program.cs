using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Net.Core.Messages;
using Net.Core.ResourceParser;
using Net.Core.Server.Connection.Identity;

BenchmarkRunner.Run<NetBenchmarks>();

public class NetBenchmarks
{
    [GlobalSetup]
    public void WarmupEngine()
    {
        INetMessage? resource = 
            ResourceConversionEngine<NetMessage<DefaultId>>
            .ParseResource("connected");
    }

    [Benchmark]
    public void BenchmarkSimpleParse()
    {
        INetMessage? resource =
            ResourceConversionEngine<NetMessage<DefaultId>>
            .ParseResource("connected");
    }

    [Benchmark]
    public void BenchmarkWith2ArgumentsAndSpecialArg()
    {
        INetMessage? resource 
            = ResourceConversionEngine<NetMessage<DefaultId>>
            .ParseResource("connected?size=14&iiwr=true");
    }
}