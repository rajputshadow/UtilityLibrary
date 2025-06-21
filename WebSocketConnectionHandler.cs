using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketHandlerLib
{
    // Base class to handle a WebSocket connection's lifecycle and messaging.
    public class WebSocketConnectionHandler
    {
        // The underlying WebSocket instance for this connection.
        private readonly WebSocket _webSocket;

        // Constructor: initializes the handler with a WebSocket instance.
        public WebSocketConnectionHandler(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }

        // Main loop for handling the WebSocket connection.
        // Receives messages, triggers lifecycle events, and manages closure.
        public async Task HandleAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Trigger the connected event (can be overridden in derived classes).
                await OnConnectedAsync();

                var buffer = new byte[1024 * 4]; // Buffer for receiving messages.

                // Loop while the WebSocket is open and cancellation is not requested.
                while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    // Wait for a message from the client.
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    // If the client requests to close the connection, exit the loop.
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    // Decode the received bytes into a string message.
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    // Trigger the message received event (can be overridden).
                    await OnMessageReceivedAsync(message);
                }

                // Trigger the disconnected event (can be overridden).
                await OnDisconnectedAsync(_webSocket.CloseStatus, _webSocket.CloseStatusDescription);
            }
            catch (Exception ex)
            {
                // Trigger the error event (can be overridden).
                await OnErrorAsync(ex);
            }
            finally
            {
                // Ensure the WebSocket is closed gracefully if still open or close was received.
                if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                }
            }
        }

        // Called when the connection is established. Override to add custom logic.
        public virtual Task OnConnectedAsync() => Task.CompletedTask;

        // Called when a message is received from the client. Override to handle messages.
        public virtual Task OnMessageReceivedAsync(string message) => Task.CompletedTask;

        // Called when the connection is closed. Override to add custom logic.
        public virtual Task OnDisconnectedAsync(WebSocketCloseStatus? status, string reason) => Task.CompletedTask;

        // Called when an error occurs. Override to handle errors.
        public virtual Task OnErrorAsync(Exception ex) => Task.CompletedTask;

        // Sends a text message to the client if the WebSocket is open.
        public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message); // Encode the message as UTF-8 bytes.
                await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
            }
        }
    }
}
