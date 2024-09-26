using GameServer;
using Microsoft.Extensions.Configuration;
using Serilog;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/server_log.txt")
            .CreateLogger();

        var server = new WebSocketServer();
        await server.StartAsync("http://localhost:5000/");
    }
}