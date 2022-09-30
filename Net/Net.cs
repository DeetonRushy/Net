using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic;
using Net.Config;
using Net.Core.Client;
using Net.Core.Logging;
using Net.Core.Messages;
using Net.Core.ResourceParser;
using Net.Core.Server;
using Net.Core.Server.Connection.Identity;
using System.Runtime.CompilerServices;
using System.Security.Principal;

namespace Net
{
    public static class Factory
    {
        static Factory()
        {
            // Initialize default configuration values

#if DEBUG
            //ConfigurationManager.SetFlag(
            //ConfigTarget.Server, new("dumpCfgOnWrite"));
            // ^^ server config gets dumped every time a value is written to it.

            ConfigurationManager.SetFlag(ConfigTarget.Server,
                new("debug")
            );
#else
            ConfigurationManager.SetFlag(ConfigTarget.Server,
                new("release")
            );
#endif
        }

        public static NetServer<T> MakeServer<T>() where T : ICLIdentifier, new()
        {
            return new (); 
        }

        public static NetClient<Msg, Id> MakeClient<Msg, Id>()
            where Msg : INetMessage<Id>
            where Id  : ICLIdentifier
        {
            var client = new NetClient<Msg, Id>();
            return client;
        }

        public static async Task<NetClient<Msg, Id>> 
            MakeClientAndStart<Msg, Id>(string ip, int port, Id id)
            where Msg: INetMessage<Id>
            where Id: ICLIdentifier
        {
            var cl = MakeClient<Msg, Id>();
#if DEBUG
            cl.UseLogger<DebugLogger>();
#endif
            cl.SetLocalIdentifier(id);
            await cl.Start(ip, port);
            return cl;
        }

        public static async Task<NetServer<T>> MakeServerFromDetails<T>() where T: ICLIdentifier, new()
        {
            if (ServerConfig.GetFlag("connection_details") is not ConfigFlag details)
            {
                throw new InvalidOperationException("Cannot MakeServerFromDetails without details set in the configuration");
            }

            var ip = details.Options[0];
            var port = int.Parse(details.Options[1]);

            return await MakeServerAndStart<T>(ip, port);
        }

        public static async Task<NetClient<Packet, Identity>> MakeClientFromDetails<Packet, Identity>(Identity id)
            where Packet : INetMessage<Identity>, new()
            where Identity : ICLIdentifier, new()
        {
            if (ClientConfig.GetFlag("connection_details") is not ConfigFlag details)
            {
                throw new InvalidOperationException("Cannot MakeServerFromDetails without details set in the configuration");
            }

            var ip = details.Options[0];
            var port = int.Parse(details.Options[1]);

            return await MakeClientAndStart<Packet, Identity>(ip, port, id);
        }    

        public static async Task<NetServer<T>> MakeServerAndStart<T>(string ip = "localhost", int port = 1337) where T : ICLIdentifier, new()
        {
            var s = MakeServer<T>();
            await s.Start(ip, port);
            return s;
        }

        public static void SetGlobalConnectionDetails(string Ip, int Port)
        {
            ConfigurationManager.SetFlag(ConfigTarget.Global,
                new ConfigFlag("connection_details",
                new List<string>()
                {
                    Ip,
                    Port.ToString()
                }));
        }

        public static async Task<T?> MessageFromResourceString<T, I>(string resource) where T : class, INetMessage<I>, new() where I: ICLIdentifier
        {
            return await Task.Run(() =>
            {
                var message = new T();
                message = ResourceConversionEngine<T, I>.ParseResource(resource);
                return message;
            });
        }
    }
}