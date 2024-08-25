using System.Text;
using System.Net.WebSockets;
using System.Collections.Concurrent;

namespace WebSocketChatApp.Services
{
    public class WebSocketHandler
    {

        private static readonly ConcurrentDictionary<string, WebSocket> _connections = new ConcurrentDictionary<string, WebSocket>();

        public static async Task HandleWebSocketAsync(WebSocket webSocket)
        {
            var connectionId = Guid.NewGuid().ToString();
            _connections.TryAdd(connectionId, webSocket);
            Console.WriteLine();

            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await BroadcastMessage(message);
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            _connections.TryRemove(connectionId, out _);
            await BroadcastMessage($"{connectionId} disconnected");
        }

        private static async Task BroadcastMessage(string message)
        {
            foreach (var connection in _connections)
            {
                if (connection.Value.State == WebSocketState.Open)
                {
                    await connection.Value.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
