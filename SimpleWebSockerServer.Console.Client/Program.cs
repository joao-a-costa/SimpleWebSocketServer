using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace SimpleWebSockerServer.Console.Client
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            string serverAddress = "ws://localhost:20005/";

            // Create a new client WebSocket instance
            var clientWebSocket = new ClientWebSocket();

            try
            {
                // Connect to the server
                await clientWebSocket.ConnectAsync(new Uri(serverAddress), CancellationToken.None);

                // Start a new thread to listen for messages from the server
                _ = Task.Run(async () =>
                {
                    byte[] receiveBuffer = new byte[1024];
                    while (clientWebSocket.State == WebSocketState.Open)
                    {
                        var receiveResult = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                        string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count);
                        System.Console.WriteLine($"Received from server: {receivedMessage}");
                    }
                });

                // Send messages to the server
                while (clientWebSocket.State == WebSocketState.Open)
                {
                    System.Console.WriteLine("Enter a message to send:");
                    string message = System.Console.ReadLine();
                    if (!string.IsNullOrEmpty(message))
                    {
                        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                        await clientWebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                // Close the WebSocket connection
                if (clientWebSocket.State == WebSocketState.Open)
                    await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
            }

            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadKey();
        }
    }
}
