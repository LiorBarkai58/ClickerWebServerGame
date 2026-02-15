using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefaultNamespace;
using UnityEngine;

namespace ChatClient
{

    /// <summary>
    /// WebSocket client for chat and matchmaking. Connects to server, sends/receives ChatMessage objects and match_found.
    /// Call ConnectAsync then poll DrainPendingMessages from Update for chat; subscribe to OnMatchFound for matchmaking.
    /// </summary>
    public class WebSocketChatClient : MonoBehaviour
    {
        public bool IsConnected => _socket?.State == WebSocketState.Open;
        
        /// <summary> Fired on main thread when server sends match_found. Argument: opponentId. </summary>
        public event Action<string> OnMatchFound;

        ClientWebSocket _socket;
        readonly ConcurrentQueue<ChatMessage> _incoming = new ConcurrentQueue<ChatMessage>();
        readonly ConcurrentQueue<string> _matchFoundQueue = new ConcurrentQueue<string>();
        CancellationTokenSource _cts;
        bool _receiveLoopRunning;

        /// <summary>
        /// Connect to WebSocket server. URL must include userId if your server expects it (e.g. ws://localhost:5000/ws?userId=Alice).
        /// </summary>
        public async void ConnectAsync(string wsUrl)
        {
            if (_socket != null)
            {
                try { await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); } catch { }
                _socket.Dispose();
            }

            _socket = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            try
            {
                _socket.Options.SetRequestHeader("Authorization", "Bearer " + ClientSession.Instance.Jwt);
                await _socket.ConnectAsync(new Uri(wsUrl), _cts.Token);
                if (_socket.State == WebSocketState.Open)
                {
                    _receiveLoopRunning = true;
                    _ = ReceiveLoopAsync();
                    Debug.Log("Connected to WebSocket server");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Chat] Connect failed: {e.Message}");
            }
        }

        async Task ReceiveLoopAsync()
        {
            var buffer = new byte[4096];
            var cts = _cts.Token;

            while (_socket != null && _socket.State == WebSocketState.Open && _receiveLoopRunning)
            {
                try
                {
                    var result = await _socket.ReceiveAsync(buffer, cts);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.Count > 0)
                    {
                        var text = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        if (TryParseMatchFound(text, out var opponentId))
                        {
                            _matchFoundQueue.Enqueue(opponentId);
                        }
                        else
                        {
                            // Parse ChatMessage JSON safely
                            try
                            {
                                var msg = JsonUtility.FromJson<ChatMessage>(text);
                                if (msg != null)
                                    _incoming.Enqueue(msg);
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"[Chat] Failed to parse ChatMessage: {e.Message}");
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Chat] Receive error: {e.Message}");
                    break;
                }
            }

            _receiveLoopRunning = false;
        }

        /// <summary>
        /// Call from Update. Returns and clears pending chat messages (safe on main thread).
        /// Also drains match_found into OnMatchFound (fire from main thread).
        /// </summary>
        public void DrainPendingMessages(System.Collections.Generic.List<ChatMessage> outMessages)
        {
            outMessages.Clear();
            while (_matchFoundQueue.TryDequeue(out var opponentId))
                OnMatchFound?.Invoke(opponentId);

            while (_incoming.TryDequeue(out var msg))
                outMessages.Add(msg);
        }

        static bool TryParseMatchFound(string text, out string opponentId)
        {
            opponentId = null;
            if (string.IsNullOrWhiteSpace(text) || text.IndexOf("match_found", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            var oppKey = "\"opponentId\"";
            var idx = text.IndexOf(oppKey, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;

            var colon = text.IndexOf(':', idx + oppKey.Length);
            if (colon < 0) return false;

            var startQ = text.IndexOf('"', colon);
            if (startQ < 0) return false;

            var endQ = text.IndexOf('"', startQ + 1);
            if (endQ < 0) return false;

            opponentId = text.Substring(startQ + 1, endQ - startQ - 1);
            return true;
        }

        /// <summary>
        /// Send find_match request to server. Safe to call from main thread.
        /// </summary>
        public async void SendFindMatch()
        {
            if (_socket?.State != WebSocketState.Open) return;
            var message = "{\"type\":\"find_match\"}";
            var bytes = Encoding.UTF8.GetBytes(message);
            try
            {
                await _socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Matchmaking] Send find_match error: {e.Message}");
            }
        }

        /// <summary>
        /// Send a text message. Safe to call from main thread.
        /// </summary>
        public async void Send(string message)
        {
            if (_socket?.State != WebSocketState.Open) return;

            // Wrap message in ChatMessage object or just send raw string content
            // Server will assign senderId and senderName
            var payload = JsonUtility.ToJson(new ChatMessage()
            {
                Message = message,
                SenderName = ClientSession.Instance.Username
            });

            
            var bytes = Encoding.UTF8.GetBytes(payload);
            try
            {
                await _socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Chat] Send error: {e.Message}");
            }
        }

        /// <summary>
        /// Disconnect and cleanup. Sends close frame first so server sees WebSocketMessageType.Close.
        /// </summary>
        public async void Disconnect()
        {
            _receiveLoopRunning = false;
            if (_socket?.State == WebSocketState.Open)
            {
                try
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                catch { }
            }
            _cts?.Cancel();
            _socket?.Dispose();
            _socket = null;
        }

        void OnDestroy()
        {
            Disconnect();
        }
    }
}
