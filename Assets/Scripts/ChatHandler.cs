 using System;
 using System.Collections.Generic;
 using ChatClient;
 using DefaultNamespace;
 using TMPro;
 using UnityEngine;

 public class ChatHandler : MonoBehaviour
    {
        [SerializeField] private WebSocketChatClient webSocketChat;

        [SerializeField] private TextMeshProUGUI textBox;
        [SerializeField] private TMP_InputField messageToSend;
        [SerializeField] private GameObject chatParent;

        private List<ChatMessage> chatBuffer = new List<ChatMessage>();
        private List<ChatMessage> messages = new List<ChatMessage>();
        private int maxMessages = 10;

        // Your client session manager holds user info
        private string myUserId => ClientSession.Instance.Username;

        

        public void Connect()
        {
            webSocketChat.ConnectAsync(URLstorage.baseWSURL + "/chat");
            chatParent.SetActive(true);//guarantee chat is enabled when connecting
        }

        public void Disconnect()
        {
            webSocketChat.Disconnect();
            messages.Clear();
        }

        private void Update()
        {
            DrainChatMessages();
            PostMessages();
        }

        private void DrainChatMessages()
        {
            // Drain ChatMessage objects directly from the WebSocket client
            var incomingMessages = new List<ChatMessage>();
            webSocketChat.DrainPendingMessages(incomingMessages);

            // Add them to the chat buffer
            chatBuffer.AddRange(incomingMessages);
        }

        private void PostMessages()
        {
            if (chatBuffer.Count == 0) return;

            foreach (var msg in chatBuffer)
            {
                if (messages.Count >= maxMessages) messages.RemoveAt(0);
                messages.Add(msg);
            }

            chatBuffer.Clear();
            UpdateChatWindow();
        }

        private void UpdateChatWindow()
        {
            // Display newest messages last
            var displayStrings = new List<string>();
            foreach (var msg in messages)
            {
                Debug.Log($"sendername: {msg.SenderName}, message: {msg.Message}");
                string displayName = msg.SenderName == myUserId ? "You" : msg.SenderName;
                displayStrings.Add($"{displayName}: {msg.Message}");
            }

            textBox.SetText(string.Join("\n", displayStrings));
        }

        [ContextMenu("Send Message")]
        public void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(messageToSend.text)) return;

            // Only send the message content; server will attach sender
            webSocketChat.Send(messageToSend.text);
            messageToSend.text = string.Empty;
        }
    }