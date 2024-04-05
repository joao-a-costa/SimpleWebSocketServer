using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace SimpleWebSocketServer
{
    public class WebSocketServer
    {
        #region "Constants"

        private const string _MessageServerStarted = "Server started";
        private const string _MessageClientConnected = "WebSocket connected";
        private const string _MessageClientDisconnected = "WebSocket disconnected";
        private const string _MessageReceivedMessageFromClient = "Received message from client";
        private const string _MessageErrorReceivingMessageFromClient = "Error receiving message from client";
        private const string _MessageWebSocketError = "WebSocket error";
        private const string _MessageErrorSendingMessageToClient = "Error sending message to client";

        #endregion

        #region "Members"

        /// <summary>
        /// The HttpListener object to listen for incoming HTTP requests
        /// </summary>
        private HttpListener _httpListener;
        /// <summary>
        /// The WebSocket object to handle WebSocket communication
        /// </summary>
        private WebSocket? _webSocket;

        #endregion

        #region "Events"

        /// <summary>
        /// Define an event to be raised when the server starts
        /// </summary>
        public event EventHandler<string>? ServerStarted;
        /// <summary>
        /// Define an event to be raised when a message is received
        /// </summary>
        public event EventHandler<string>? MessageReceived;
        /// <summary>
        /// Define an event to be raised when a client is connected
        /// </summary>
        public event EventHandler<string>? ClientConnected;
        /// <summary>
        /// Define an event to be raised when a client is disconnected
        /// </summary>
        public event EventHandler<string>? ClientDisconnected;

        #endregion

        #region "Properties"

        /// <summary>
        /// Define a property to enable/disable debug mode
        /// </summary>
        private bool DebugMode { get; set; } = false;

        #endregion

        #region "Constructor"

        /// <summary>
        /// Constructor to initialize the WebSocket server
        /// </summary>
        /// <param name="prefix"></param>
        public WebSocketServer(string prefix)
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(prefix);
        }

        #endregion

        #region "Public"

        public async Task Start()
        {
            _httpListener.Start();

            OnServerStarted(_MessageServerStarted);

            while (true)
            {
                HttpListenerContext context = await _httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    await ProcessWebSocketRequest(context);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        /// <summary>
        /// The method to process the WebSocket request
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            HttpListenerWebSocketContext? webSocketContext = null;

            try
            {
                webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                _webSocket = webSocketContext.WebSocket;

                // Raise the event for message received
                OnClientConnected(_MessageClientConnected);

                // Start echoing messages
                await Task.WhenAll(Task.Run(() => SendConsoleInputToClient(_webSocket)), Task.Run(() => ReceiveMessagesFromClient(_webSocket)));

            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
                Log($"{_MessageWebSocketError}: {ex.Message}");
            }
            finally
            {
                if (webSocketContext != null)
                    webSocketContext.WebSocket.Dispose();

                OnClientDisconnected(_MessageClientDisconnected);
            }
        }

        /// <summary>
        /// The method to send console input to the client
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private async Task SendConsoleInputToClient(WebSocket webSocket)
        {
            try
            {
                while (_webSocket?.State == WebSocketState.Open)
                {
                    // Read input from the console
                    string? input = Console.ReadLine();

                    if (!string.IsNullOrEmpty(input))
                    {
                        // Send input to the client
                        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                        await webSocket.SendAsync(new ArraySegment<byte>(inputBytes),
                            WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"{_MessageErrorSendingMessageToClient}: {ex.Message}");
            }
        }

        /// <summary>
        /// The method to receive messages from the client
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private async Task ReceiveMessagesFromClient(WebSocket webSocket)
        {
            try
            {
                byte[] buffer = new byte[1024];

                while (_webSocket?.State == WebSocketState.Open)
                {
                    // Receive message from the client
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    // Process received message from the client
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"{_MessageReceivedMessageFromClient}: {receivedMessage}");

                    // Raise the event for message received
                    OnMessageReceived(receivedMessage);
                }
            }
            catch (Exception ex)
            {
                Log($"Error receiving message from client: {ex.Message}");
            }
        }

        #endregion

        #region "Private"

        /// <summary>
        /// Method to log messages to the console
        /// </summary>
        /// <param name="message"></param>
        private void Log(string message)
        {
            if (DebugMode)
            {
                Console.WriteLine(message);
            }
        }

        #endregion

        #region "Events"

        /// <summary>
        /// Method to raise the ServerStarted event
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnServerStarted(string message)
        {
            Log(message);
            ServerStarted?.Invoke(this, message);
        }

        /// <summary>
        /// Method to raise the MessageReceived event
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnMessageReceived(string message)
        {
            Log(message);
            MessageReceived?.Invoke(this, message);
        }

        /// <summary>
        /// Method to raise the ClientConnected event
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnClientConnected(string message)
        {
            Log(message);
            ClientConnected?.Invoke(this, message);
        }

        /// <summary>
        /// Method to raise the ClientDisconnected event
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnClientDisconnected(string message)
        {
            Log(message);
            ClientDisconnected?.Invoke(this, message);
        }

        #endregion
    }
}
