using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleWebSocketServer.Console.Server
{
    internal static class Program
    {
        #region "Constants"

        private const int _WebSocketServerPrefixPort = 10005;
        private const string _WebSocketServerPrefixHttp = "http";
        private const string _WebSocketServerPrefixHttps = "https";
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
        /// <summary>
        /// The flag indicating whether to use SSL.
        /// </summary>
        private static bool _useSsl = true;
        private static List<Guid> _clients = new List<Guid>();

        #endregion

        #region "Private Methods"

        /// <summary>
        /// Gets the certificate temporary path.
        /// </summary>
        /// <returns>The certificate temporary path.</returns>
        private static string GetCertificateTempPath()
        {
            var tempFileName = Path.GetRandomFileName();

            File.WriteAllBytes(tempFileName, SimpleWebSockerServer.Console.Server.Properties.Resources.HostSimulator);

            return tempFileName;
        }

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
                    await server.SendMessageToClient(_clients.FirstOrDefault(), input);
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
            var prefix = $"{(_useSsl ? _WebSocketServerPrefixHttps : _WebSocketServerPrefixHttp)}://+:{_WebSocketServerPrefixPort}/";
            var certificateTempPath = string.Empty;
            var certificateTempPathPassword = string.Empty;

            if (_useSsl)
            {
                certificateTempPath = GetCertificateTempPath();
                certificateTempPathPassword = "mypass";

                server = new WebSocketServer(prefix);
            }
            else
            {
                
                server = new WebSocketServer(prefix);
            }

            try
            {
                server.ClientConnected += Server_ClientConnected;
                server.ClientDisconnected += Server_ClientDisconnected;

                System.Console.WriteLine(_MessageEnterJSONCommand);

                if (_useSsl)
                    await server.InstallCertificate(certificateTempPath, certificateTempPathPassword, string.Empty, string.Empty);

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

                if (File.Exists(certificateTempPath))
                {
                    File.Delete(certificateTempPath);
                }
            }

            System.Console.WriteLine(_MessagePressAnyKeyToExit);
            System.Console.ReadKey();
        }

        private static void Server_ClientDisconnected(object sender, (Guid clientId, string message) e)
        {
            if (_clients.Contains(e.clientId))
            {
                _clients.Remove(e.clientId);
            }
            System.Console.WriteLine($"Client disconnected: {e.clientId}");
        }

        private static void Server_ClientConnected(object sender, (Guid, string) e)
        {
            _clients.Add(e.Item1);
            System.Console.WriteLine($"Client connected: {e.Item1}");
        }
    }
}