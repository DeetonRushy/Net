﻿using Net;
using Net.Config;
using Net.Core.Logging;
using Net.Core.Messages;
using Net.Core.ResourceParser.Lexer;
using Net.Core.Server.Connection.Identity;

/*
 * creates a local server + client
 */

ConfigurationManager.UseLogger<DebugLogger>();

Factory.SetGlobalConnectionDetails("localhost", 56433);

var identity = new DefaultId("Deeton");

var server = await Factory.MakeServerFromDetails<DefaultId>();
var client = await Factory.MakeClientFromDetails<NetMessage<DefaultId>, DefaultId>(identity);

client.On("connected", (args) =>
{
    Console.WriteLine("connected to the server!");
});

var msg = await Factory.MessageFromResourceString<NetMessage<DefaultId>>("display?text='Willy And Balls'");

if (msg is null)
{
    return;
}

await server.RhetoricalSendTo
    (IdentityType.Name, "Deeton",
    msg);

Console.ReadLine();