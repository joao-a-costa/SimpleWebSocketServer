using SimpleWebSocketServer;

/// Define the WebSocket server prefix
string prefix = "http://localhost:20005/";

// Create an instance of WebSocketServer
var server = new WebSocketServer(prefix);

try
{
    /// Define an event to be raised when the server starts
    server.ServerStarted += (sender, message) =>
    {
        Console.WriteLine($"{message}");
    };
    /// Define an event to be raised when a message is received
    server.ClientConnected += (sender, message) =>
    {
        Console.WriteLine($"{message}");
    };
    /// Define an event to be raised when a message is received
    server.MessageReceived += (sender, message) =>
    {
        Console.WriteLine($"{message}");
    };

    // Start the WebSocket server
    server.Start().Wait();
}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred: {ex.Message}");
}

Console.WriteLine("Press any key to exit...");
Console.ReadKey();