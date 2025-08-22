using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Library;
using Nexus.Library.Components;
using Nexus.Library.Modules;

namespace Nexus.Runtime;

class Program
{
    static void Main(string[] args)
    {
        // RootCommand rootCommand = new("DaprRapr Runtime")
        // {
        //     new Option<string>("--components")
        //     {
        //         Description = "Location of the components ."
        //     },
        //     new Option<int>("--grpc-port")
        //     {
        //         Description = "An option whose argument is parsed as a string."
        //     }
        // };
        //
        // ParseResult parseResult = rootCommand.Parse(args);
        // var grpcPort = parseResult.GetValue<int>("--grpc-port");
        // var components = parseResult.GetValue<string>("--components");

        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);
        builder.Configuration.AddCommandLine(args);

        builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));

        builder.Services.AddSingleton(new Meter(builder.Environment.ApplicationName, "1.0.0"));

        // var config = builder.Configuration;
        // var components = config["Components"];
        builder.Services.AddSingleton<Manager>(provider => new Manager(
            provider.GetService<IOptions<Settings>>()?.Value.Components!,
            provider.GetService<ILogger<Program>>()!, provider.GetService<Meter>()!));

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(int.Parse(builder.Configuration.GetSection("Settings")["GrpcPort"]!),
                o => o.Protocols = HttpProtocols.Http2);
        });

        builder.Services.AddGrpc();

        var app = builder.Build();

        ComponentFactory.RegisterComponents(app.Services.GetService<ILogger<Program>>());
        app.Services.GetRequiredService<Manager>(); //start up scheduler and subscribers

        app.UseRouting();
        app.MapGrpcService<CallerService>();
        app.Run();
    }
}