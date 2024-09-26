using System.Net;
using System.Net.WebSockets;
using Serilog;
using System.Text;

namespace GameServer
{
    public class WebSocketServer
    {
        private readonly PlayerManager _playerManager = new PlayerManager();
        private readonly MessageRouter _messageRouter;

        public WebSocketServer()
        {
            _messageRouter = new MessageRouter(_playerManager);
        }

        public async Task StartAsync(string url)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Log.Information("WebSocket server started at {0}", url);

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    // Handle each WebSocket connection in a separate task to allow concurrency
                    _ = Task.Run(() => ProcessRequestAsync(context));
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            WebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
            WebSocket socket = webSocketContext.WebSocket;

            Log.Information("New WebSocket connection established.");

            byte[] buffer = new byte[1024];

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Handle client-initiated WebSocket close request
                        Log.Information("Client initiated WebSocket close.");
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                        break;
                    }

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Log.Information("Received message: {0}", receivedMessage);

                    // Route the received message to the appropriate handler
                    string response = await _messageRouter.RouteMessageAsync(receivedMessage, socket);

                    // Send response back to the client
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    await socket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (WebSocketException ex)
            {
                Log.Error("WebSocket exception: {0}", ex.Message);
            }
            finally
            {
                // Ensure the WebSocket connection is closed if the client disconnects unexpectedly
                if (socket.State != WebSocketState.Closed)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Server error", CancellationToken.None);
                }
                socket.Dispose();
                Log.Information("WebSocket connection closed.");
            }
        }
    }
}