﻿using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MessagePack;
using System.Threading.Channels;

Console.WriteLine("Insert the URL of the SignalR hub");
string? url = Console.ReadLine();

Console.WriteLine("please insert token:");
var token = Console.ReadLine();

var hubConnection = new HubConnectionBuilder()
                         .WithUrl(url,
                            HttpTransportType.WebSockets,
                            options => {
                                options.AccessTokenProvider = null;
                                options.HttpMessageHandlerFactory = null;
                                options.Headers["CustomData"] = "value";
                                options.SkipNegotiation = true;
                                options.ApplicationMaxBufferSize = 1_000_000;
                                options.ClientCertificates = new System.Security.Cryptography.X509Certificates.X509CertificateCollection();
                                options.CloseTimeout = TimeSpan.FromSeconds(5);
                                options.Cookies = new System.Net.CookieContainer();
                                options.DefaultTransferFormat = TransferFormat.Text;
                                options.Credentials = null;
                                options.Proxy = null;
                                options.UseDefaultCredentials = true;
                                options.TransportMaxBufferSize = 1_000_000;
                                options.WebSocketConfiguration = null;
                                options.WebSocketFactory = null;
                                options.AccessTokenProvider = () => Task.FromResult(token);
                            })
                         .ConfigureLogging(logging => {
                             logging.SetMinimumLevel(LogLevel.Information);
                             //logging.AddConsole();
                         })
                         .AddMessagePackProtocol(options => 
                            {
                                options.SerializerOptions = MessagePackSerializerOptions.Standard
                                    .WithSecurity(MessagePackSecurity.UntrustedData)
                                    .WithCompression(MessagePackCompression.Lz4Block)
                                    .WithAllowAssemblyVersionMismatch(true)
                                    .WithOldSpec()
                                    .WithOmitAssemblyVersion(true);
                            })
                         .Build();

hubConnection.HandshakeTimeout = TimeSpan.FromSeconds(15);
hubConnection.ServerTimeout = TimeSpan.FromSeconds(30);
hubConnection.KeepAliveInterval = TimeSpan.FromSeconds(10);


hubConnection.On<string>("ReceiveMessage", message => Console.WriteLine($"SignalR Hub message: {message}"));



try
{
    await hubConnection.StartAsync();
    bool running = true;
    while(running)
    {
        string? message = string.Empty;
        string? groupName = string.Empty;

        Console.WriteLine( "Please specify the action:" );
        Console.WriteLine( "0 - broadcast to all" );
        Console.WriteLine( "1 - send to others" );
        Console.WriteLine( "2 - send to self" );
        Console.WriteLine( "3 - send to individual");
        Console.WriteLine( "4 - send to a group");
        Console.WriteLine( "5 - add user to a group");
        Console.WriteLine( "6 - remove user from a group");
        Console.WriteLine( "7 - trigger a stream server");
        Console.WriteLine( "exit - Exit the program" );

        string? action = Console.ReadLine();

        if (action != "5" && action != "6")
        {
            Console.WriteLine("Please specify the message");
            message = Console.ReadLine();
        }
        if (action == "4" || action == "5" || action == "6")
        {
            Console.WriteLine("Please specify the group name");
            groupName = Console.ReadLine();
        }

        switch (action)
        {
            case "0":
                if(message?.Contains(';') ?? false)
                {
                    var channel = Channel.CreateBounded<string>(10);
                    await hubConnection.SendAsync("BroadcastStream", channel.Reader);

                    foreach(string item in message.Split(';'))
                    {
                        await channel.Writer.WriteAsync(item);
                    }

                    channel.Writer.Complete();
                }
                else
                {
                    await hubConnection.SendAsync("BroadcastMessage", message);
                }
                break;
            case "1":
                await hubConnection.SendAsync("SendToOthers", message);
                break;
            case "2":
                await hubConnection.SendAsync("SendToSelf", message);
                break;
            case "3":
                Console.WriteLine("Please specify the connection id:");
                var connectionId = Console.ReadLine();
                await hubConnection.SendAsync("SendToIndividual ", connectionId, message);
                break;
            case "4":
                hubConnection.SendAsync("SendToGroup", groupName, message).Wait();
                break;
            case "5":
                hubConnection.SendAsync("AddUserToGroup", groupName).Wait();
                break;
            case "6":
                hubConnection.SendAsync("RemoveUserFromGroup", groupName).Wait();
                break;
            case "7":
                Console.WriteLine("please specify the number of jobs to execute");
                var numberOfJobs = int.Parse(Console.ReadLine() ?? "0");
                var cancellationTokenSource = new CancellationTokenSource();
                var stream = hubConnection.StreamAsync<string>(
                    "TriggerStream", numberOfJobs, cancellationTokenSource.Token);
                
                await foreach(var reply in stream)
                {
                    Console.WriteLine(reply);
                }
                break;
            case "exit":
                running = false;
                break;
            default:
                Console.WriteLine("Undefined command!");
                break;
        }
        //await hubConnection.SendAsync("BroadCastMessage", message);
    }
}
catch (System.Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    return;
}