namespace Unet.EventDrivenChat.MessagesHandler
{
    public class JoinMessageHandler : BaseMessageHandler
    {
        public override MessageType HandledMessageType => MessageType.Join;

        public JoinMessageHandler(ChatRoom room) : base(room) { }

        public override void HandleMessage(ChatMessage message)
        {
            LogHandling(message);
            
            if (chatRoom.IsHost)
            {
                HandleJoinRequestAsHost(message);
            }
            else if (message.FromName == chatRoom.HostName || message.Content == "HostResponse")
            {
                HandleJoinResponseAsClient(message);
            }
        }

        private void HandleJoinRequestAsHost(ChatMessage message)
        {
            // 1. 添加新用戶
            chatRoom.AddUser(message.FromName, message.FromIP, message.FromPort);

            // 2. 發送主機身份回應
            var joinResponse = new ChatMessage
            {
                FromName = chatRoom.UserName,
                ToName = message.FromName,
                Type = MessageType.Join,
                Content = "HostResponse",
                FromPort = chatRoom.LocalPort,
                FromIP = chatRoom.LocalIP
            };

            chatRoom.SendNetworkDirectMessage(joinResponse, message.FromIP, message.FromPort);

            // 3. 發送歡迎消息
            var welcomeMessage = new ChatMessage
            {
                FromName = "系統",
                Type = MessageType.System,
                Content = $"歡迎 {message.FromName} 加入聊天室！",
                FromPort = chatRoom.LocalPort,
                FromIP = chatRoom.LocalIP
            };

            // 4. 先更新主機的用戶列表顯示
            ChatEvents.RaiseUserListUpdated(chatRoom.GetUserList());
            
            // 5. 廣播歡迎消息和用戶列表
            chatRoom.BroadcastMessage(welcomeMessage);
            chatRoom.BroadcastUserList();
        }

        private void HandleJoinResponseAsClient(ChatMessage message)
        {
            if (message.Content == "HostResponse")
            {
                chatRoom.SetHostName(message.FromName);
                chatRoom.AddUser(message.FromName, message.FromIP, message.FromPort);
                chatRoom.SetInRoom(true);
                ChatEvents.RaiseRoomStateChanged(true);
                ChatEvents.RaiseSystemMessage($"成功加入房間，主機: {message.FromName}");
            }
        }
    }
} 