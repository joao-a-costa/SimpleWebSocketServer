using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SimpleWebSocketServer
{
    public class WebSocketServer
    {
        #region "Constants"

        private const string _MessageServerStarted = "Server started";
        private const string _MessageServerStop = "Server stop";
        private const string _MessageClientConnected = "WebSocket connected";
        private const string _MessageClientDisconnected = "WebSocket disconnected";
        private const string _MessageSentMessageToClient = "Sent message to client";
        private const string _MessageReceivedMessageFromClient = "Received message from client";
        private const string _MessageWebSocketError = "WebSocket error";
        private const string _MessageWebSocketConnectionClosedByClient = "WebSocket connection closed by client";
        private const string _MessageClosing = "Closing";
        private const string _MessageClosingDueToError = "Closing due to error";
        private const string _MessageErrorSendingMessageToClient = "Error sending message to client";
        private const string _MessageErrorReceivingMessageFromClient = "Error receiving message from client";
        private const string _MessageErrorConnectionClosedPrematurely = "Connection closed prematurely";
        private const string _MessageErrorWebSocketIsNotConnected = "WebSocket is not connected";
        private const int _BufferSize = 2050;

        #endregion

        #region "Members"

        /// <summary>
        /// The HttpListener object to listen for incoming HTTP requests
        /// </summary>
        private readonly HttpListener _httpListener;
        /// <summary>
        /// The WebSocket object to handle WebSocket communication
        /// </summary>
        private WebSocket _webSocket;

        #endregion

        #region "Events"

        /// <summary>
        /// Define an event to be raised when the server starts
        /// </summary>
        public event EventHandler<string> ServerStarted;
        /// <summary>
        /// Define an event to be raised when a message is received
        /// </summary>
        public event EventHandler<string> MessageReceived;
        /// <summary>
        /// Define an event to be raised when a client is connected
        /// </summary>
        public event EventHandler<string> ClientConnected;
        /// <summary>
        /// Define an event to be raised when a client is disconnected
        /// </summary>
        public event EventHandler<string> ClientDisconnected;

        #endregion

        #region "Properties"

        /// <summary>
        /// Property to check if the server is started
        /// </summary>
        public bool IsStarted => _httpListener.IsListening;

        public int BufferSize => _BufferSize;

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

        /// <summary>
        /// Start the WebSocket server
        /// </summary>
        /// <returns>The task to start the server</returns>
        public async Task Start()
        {
            _httpListener.Start();

            OnServerStarted(_MessageServerStarted);

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

        /// <summary>
        /// Stop the WebSocket server
        /// </summary>
        /// <returns>The task to stop the server</returns>
        public async Task Stop()
        {
            if (_webSocket != null)
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, _MessageClosing, CancellationToken.None);

            if (IsStarted)
            {
                _httpListener.Stop();
                _httpListener.Close();
            }

            Log(_MessageServerStop);
        }

        /// <summary>
        /// Send a message to the client
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>The task to send the message</returns>
        public async Task SendMessageToClient(string message)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                Log($"{_MessageSentMessageToClient}: {message}");
            }
            else
            {
                Log(_MessageErrorWebSocketIsNotConnected);
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
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]{message}");
        }

        /// <summary>
        /// Process the WebSocket request
        /// </summary>
        /// <param name="context">The HttpListenerContext object</param>
        /// <returns>The task to process the WebSocket request</returns>
        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            HttpListenerWebSocketContext webSocketContext = null;

            try
            {
                webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                _webSocket = webSocketContext.WebSocket;

                // Raise the event for client connected
                OnClientConnected(_MessageClientConnected);

                // Start echoing messages
                var sendTask = Task.Run(() => SendConsoleInputToClient(_webSocket));
                var receiveTask = Task.Run(() => ReceiveMessagesFromClient(_webSocket));

                // Wait for both tasks to complete
                await Task.WhenAny(sendTask, receiveTask);
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
                {
                    if (webSocketContext.WebSocket.State == WebSocketState.Open)
                    {
                        await webSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, _MessageClosing, CancellationToken.None);
                    }
                    webSocketContext.WebSocket.Dispose();
                }

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
                    string input = Console.ReadLine();

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
                byte[] buffer = new byte[BufferSize];

                while (_webSocket?.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = null;
                    try
                    {
                        // Receive message from the client
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    }
                    catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                    {
                        // Handle the specific case where the connection is closed prematurely
                        Log($"{_MessageErrorReceivingMessageFromClient}: {_MessageErrorConnectionClosedPrematurely}. {ex.Message}");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Close the WebSocket connection
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, _MessageClosing, CancellationToken.None);
                        Log(_MessageWebSocketConnectionClosedByClient);
                        break;
                    }

                    // Process received message from the client
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Log($"{_MessageReceivedMessageFromClient}: {receivedMessage}");

                    // Raise the event for message received
                    OnMessageReceived(receivedMessage);
                }
            }
            catch (Exception ex)
            {
                Log($"{_MessageErrorReceivingMessageFromClient}: {ex.Message}");
            }
            finally
            {
                // Ensure the WebSocket is closed
                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, _MessageClosingDueToError, CancellationToken.None);
                }

                // Dispose of the WebSocket
                webSocket?.Dispose();
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