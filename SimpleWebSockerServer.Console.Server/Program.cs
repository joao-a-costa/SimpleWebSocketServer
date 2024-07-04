using System;
using SimpleWebSocketServer;

namespace SimpleWebSockerServer.Console.Server
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            /// Define the WebSocket server prefix
            string prefix = "http://+:10005/";

            // Create an instance of WebSocketServer
            var server = new WebSocketServer(prefix);

            try
            {
                /// Define an event to be raised when the server starts
                server.ServerStarted += (sender, message) =>
                {
                    System.Console.WriteLine($"{message}");
                };
                /// Define an event to be raised when a message is received
                server.ClientConnected += (sender, message) =>
                {
                    System.Console.WriteLine($"{message}");
                };
                /// Define an event to be raised when a message is received
                server.MessageReceived += (sender, message) =>
                {
                    System.Console.WriteLine($"{message}");
                };

                // Start the WebSocket server
                server.Start().Wait();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error occurred: {ex.Message}");
            }

            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadKey();
        }
    }
}
