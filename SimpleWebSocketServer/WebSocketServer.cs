using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using SimpleWebSocketServer.Lib.Utilities;
using System.Collections.Concurrent;

namespace SimpleWebSocketServer
{
    public class WebSocketServer
    {
        #region "Constants"

        private const string _MessageServerStarted = "Server started";
        private const string _MessageServerStop = "Server stop";
        private const string _MessageSentMessageToClient = "Sent message to client";
        private const string _MessageWebSocketError = "WebSocket error";
        private const string _MessageWebSocketConnectionClosedByClient = "WebSocket connection closed by client";
        private const string _MessageClosing = "Closing";
        private const string _MessageClosingDueToError = "Closing due to error";
        private const string _MessageErrorReceivingMessageFromClient = "Error receiving message from client";
        private const string _MessageErrorConnectionClosedPrematurely = "Connection closed prematurely";
        private const string _MessageErrorWebSocketIsNotConnected = "WebSocket is not connected";
        private const string _MessageHttpListenerException = "HttpListenerException";
        private const string _MessageException = "Exception";
        private const int _BufferSize = 2050;

        #endregion

        #region "Members"

        /// <summary>
        /// The prefix for the WebSocket server
        /// </summary>
        private readonly string _prefix;
        /// <summary>
        /// The HttpListener object to listen for incoming HTTP requests
        /// </summary>
        private readonly HttpListener _httpListener;
        /// <summary>
        /// The WebSocket objects to handle WebSocket communication
        /// </summary>
        private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new ConcurrentDictionary<Guid, WebSocket>();

        #endregion

        #region "Events"

        /// <summary>
        /// Define an event to be raised when the server starts
        /// </summary>
        public event EventHandler<string> ServerStarted;
        /// <summary>
        /// Define an event to be raised when a message is received
        /// </summary>
        public event EventHandler<(Guid clientId, string message)> MessageReceived;
        /// <summary>
        /// Define an event to be raised when a client is connected
        /// </summary>
        public event EventHandler<(Guid, string)> ClientConnected;
        /// <summary>
        /// Define an event to be raised when a client is disconnected
        /// </summary>
        public event EventHandler<(Guid clientId, string message)> ClientDisconnected;
        /// <summary>
        /// Define an event to be raised when a message related to installing a certificate is received
        /// </summary>
        public static event EventHandler<string> InstallCertificateMessage;

        #endregion

        #region "Properties"

        /// <summary>
        /// Property to check if the server is started
        /// </summary>
        public bool IsStarted => _httpListener.IsListening;

        public static int BufferSize => _BufferSize;

        #endregion

        #region "Constructor"

        /// <summary>
        /// Constructor to initialize the WebSocket server
        /// </summary>
        /// <param name="prefix"></param>
        public WebSocketServer(string prefix)
        {
            _prefix = prefix;

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(_prefix);
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

            while (_httpListener.IsListening)
            {
                try
                {
                    HttpListenerContext context = await _httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        // Process the WebSocket request in a separate task
                        _ = Task.Run(() => ProcessWebSocketRequest(context));
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (HttpListenerException ex)
                {
                    // Handle the exception (e.g., log it or notify about the error)
                    Console.WriteLine($"{_MessageHttpListenerException}: {ex.Message}");
                    break; // Exit the loop if the listener stops
                }
                catch (TaskCanceledException)
                {
                    // This could occur when stopping the server, safely exit the loop
                    break;
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Console.WriteLine($"{_MessageException}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Stop the WebSocket server
        /// </summary>
        /// <returns>The task to stop the server</returns>
        public async Task Stop()
        {
            foreach (var client in _clients.Values)
            {
                if (client.State == WebSocketState.Open)
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, _MessageClosing, CancellationToken.None);
                    client.Dispose();
                }
            }
            _clients.Clear();

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
        public async Task SendMessageToClient(Guid clientId, string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var client = _clients[clientId];

            if (client.State == WebSocketState.Open)
            {
                await client.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                Log($"{_MessageSentMessageToClient}: {message}");
            }
            else
            {
                Log(_MessageErrorWebSocketIsNotConnected);
            }
        }

        public static async Task<bool> InstallCertificate(string prefix, string certificatePath, string certificatePassword, string appId,
            string certHash)
        {
            var res = false;

            await Task.Run(() =>
            {
                try
                {
                    InstallCertificateMessage?.Invoke(null, $"Installing certificate start{Environment.NewLine}{Environment.NewLine}");

                    InstallCertificateMessage?.Invoke(null, $"Importing certificate start{Environment.NewLine}");
                    var importCertificateResult = SslCertificate.ImportSslCertificate(certificatePath, certificatePassword, prefix, certHash, appId );
                    InstallCertificateMessage?.Invoke(null, importCertificateResult.Item2);
                    InstallCertificateMessage?.Invoke(null, $"Importing certificate end {Environment.NewLine}{Environment.NewLine}");

                    if (importCertificateResult.Item1)
                    {
                        InstallCertificateMessage?.Invoke(null, $"Binding certificate start{Environment.NewLine}");
                        InstallCertificateMessage?.Invoke(null,
                            SslCertificate.BindSslCertificate(prefix, certHash, appId).Item2);
                        InstallCertificateMessage?.Invoke(null, $"Binding certificate end{Environment.NewLine} {Environment.NewLine}");

                        res = true;
                    }
                }
                catch(Exception ex)
                {
                    InstallCertificateMessage?.Invoke(null, $"Installing certificate error. {_MessageException}: {ex.Message}");
                    Console.WriteLine($"{_MessageException}: {ex.Message}");
                }
                finally
                {
                    InstallCertificateMessage?.Invoke(null, $"Installing certificate end");
                }
            });

            return res;
        }

        public async Task<bool> InstallCertificate(string certificatePath, string certificatePassword, string appId, string certificateThumbprint) =>
            await InstallCertificate(_prefix, certificatePath, certificatePassword, appId, certificateThumbprint);

        #endregion

        #region "Private"

        /// <summary>
        /// Method to log messages to the console
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message)
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
            var clientId = Guid.NewGuid();
            WebSocket webSocket = null;

            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                webSocket = webSocketContext.WebSocket;
                _clients[clientId] = webSocket;

                OnClientConnected(clientId, $"Client {clientId} connected");

                await ReceiveMessagesFromClient(clientId, webSocket);
            }
            catch (Exception ex)
            {
                Log($"{_MessageWebSocketError}: {ex.Message}");
            }
            finally
            {
                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, _MessageClosing, CancellationToken.None);
                }
                _clients.TryRemove(clientId, out _);
                OnClientDisconnected(clientId, $"Client {clientId} disconnected");
            }
            //HttpListenerWebSocketContext webSocketContext = null;

            //try
            //{
            //    webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
            //    _webSocket = webSocketContext.WebSocket;

            //    // Raise the event for client connected
            //    OnClientConnected(_MessageClientConnected);

            //    // Start echoing messages
            //    var receiveTask = Task.Run(() => ReceiveMessagesFromClient(_webSocket));

            //    // Wait for both tasks to complete
            //    await Task.WhenAny(receiveTask);
            //}
            //catch (Exception ex)
            //{
            //    context.Response.StatusCode = 500;
            //    context.Response.Close();
            //    Log($"{_MessageWebSocketError}: {ex.Message}");
            //}
            //finally
            //{
            //    if (webSocketContext != null)
            //    {
            //        if (webSocketContext.WebSocket.State == WebSocketState.Open)
            //        {
            //            await webSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, _MessageClosing, CancellationToken.None);
            //        }
            //        webSocketContext.WebSocket.Dispose();
            //    }

            //    OnClientDisconnected(_MessageClientDisconnected);
            //}
        }

        /// <summary>
        /// The method to receive messages from the client
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private async Task ReceiveMessagesFromClient(Guid clientId, WebSocket webSocket)
        {
            try
            {
                byte[] buffer = new byte[BufferSize];

                var _webSocket = _clients[clientId];

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

                    // Raise the event for message received
                    OnMessageReceived(clientId, Encoding.UTF8.GetString(buffer, 0, result.Count));
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
        protected virtual void OnMessageReceived(Guid clientId, string message)
        {
            Log($"ClientID: {clientId}. Message:{message}");
            MessageReceived?.Invoke(this, (clientId, message));
        }

        /// <summary>
        /// Method to raise the ClientConnected event
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnClientConnected(Guid clientId, string message)
        {
            Log($"ClientID: {clientId}. Message:{message}");
            ClientConnected?.Invoke(this, (clientId, message));
        }

        /// <summary>
        /// Method to raise the ClientDisconnected event
        /// </summary>
        /// <param name="message"></param>
        protected virtual void OnClientDisconnected(Guid clientId, string message)
        {
            Log($"ClientID: {clientId}. Message:{message}");
            ClientDisconnected?.Invoke(this, (clientId, message));
        }

        #endregion
    }
}