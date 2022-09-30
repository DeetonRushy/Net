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
        INetMessage<DefaultId>? resource = 
            ResourceConversionEngine<NetMessage<DefaultId>, DefaultId>
            .ParseResource("connected");
    }

    [Benchmark]
    public void BenchmarkSimpleParse()
    {
        INetMessage<DefaultId>? resource =
            ResourceConversionEngine<NetMessage<DefaultId>, DefaultId>
            .ParseResource("connected");
    }

    [Benchmark]
    public void BenchmarkWith2ArgumentsAndSpecialArg()
    {
        INetMessage<DefaultId>? resource 
            = ResourceConversionEngine<NetMessage<DefaultId>, DefaultId>
            .ParseResource("connected?size=14&iiwr=true");
    }

    [Benchmark]
    public void BenchmarkWithStringLiteral()
    {
        INetMessage<DefaultId>? resource
            = ResourceConversionEngine<NetMessage<DefaultId>, DefaultId>
            .ParseResource("connected?text='Welcome!'&fontsize=14");
    }
}