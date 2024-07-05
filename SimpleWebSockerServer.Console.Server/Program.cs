using System;
using System.Threading.Tasks;

namespace SimpleWebSocketServer.Console.Server
{
    internal static class Program
    {
        #region "Constants"

        private const string _WebSocketServerPrefix = "http://+:10005/";
        private const string _MessageEnterJSONCommand = "Enter JSON command or 'q' to stop:";
        private const string _MessageErrorErrorOccurred = "Error occurred";
        private const string _MessageErrorProcessingJSON = "Error processing JSON";
        private const string _MessagePressAnyKeyToExit = "Press any key to exit...";
        private const string _MessageStoppingTheServer = "Stopping the server...";

        #endregion

        #region "Members"

        /// <summary>
        /// The WebSocket server instance.
        /// </summary>
        private static WebSocketServer server;

        #endregion

        #region "Private Methods"

        /// <summary>
        /// Listens for user input and sends the input to the WebSocket server.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private static async Task ListenForUserInput()
        {
            while (true)
            {
                // Read the entire line of input asynchronously
                string input = await Task.Run(() => System.Console.ReadLine());

                // Check if the input is 'q' to stop the server
                if (input.ToLower() == "q")
                {
                    if (server != null && server.IsStarted)
                    {
                        System.Console.WriteLine(_MessageStoppingTheServer);
                        await server.Stop(); // Stop the WebSocket server
                    }
                    break; // Exit the loop
                }

                // Process the JSON input
                try
                {
                    await server.SendMessageToClient(input);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"{_MessageErrorProcessingJSON}: {ex.Message}");
                }
            }
        }

        #endregion

        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        static async Task Main(string[] args)
        {
            // Define the WebSocket server prefix
            string prefix = _WebSocketServerPrefix;

            // Create an instance of WebSocketServer
            server = new WebSocketServer(prefix);

            try
            {
                System.Console.WriteLine(_MessageEnterJSONCommand);

                // Start the WebSocket server asynchronously
                Task serverTask = server.Start();

                // Listen for user input asynchronously
                Task userInputTask = ListenForUserInput();

                // Wait for either the user to press 'q' or the WebSocket server to stop
                await Task.WhenAny(serverTask, userInputTask);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"{_MessageErrorErrorOccurred}: {ex.Message}");
            }
            finally
            {
                // Ensure to stop the server if it's running
                if (server.IsStarted)
                {
                    await server.Stop();
                }
            }

            System.Console.WriteLine(_MessagePressAnyKeyToExit);
            System.Console.ReadKey();
        }
    }
}