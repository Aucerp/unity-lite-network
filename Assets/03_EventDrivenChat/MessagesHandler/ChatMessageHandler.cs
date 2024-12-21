using UnityEngine;

namespace Unet.EventDrivenChat.MessagesHandler
{
    public class ChatMessageHandler : BaseMessageHandler
    {
        public override MessageType HandledMessageType => MessageType.Chat;

        public ChatMessageHandler(ChatRoom room) : base(room) { }

        public override void HandleMessage(ChatMessage message)
        {
            LogHandling(message);
            
            Debug.Log($"處理聊天消息: 來自={message.FromName}, 內容={message.Content}");
            
            // 顯示消息
            ChatEvents.RaiseMessageReceived(message);
            
            // 如果是主機收到客戶端的消息，需要轉發給其他客戶端
            if (chatRoom.IsHost && message.FromName != chatRoom.UserName)
            {
                Debug.Log($"主機轉發消息給其他客戶端");
                foreach (var user in chatRoom.GetUserList())
                {
                    if (user != chatRoom.UserName && user != message.FromName)
                    {
                        var userInfo = chatRoom.GetUserInfo(user);
                        if (userInfo != null)
                        {
                            Debug.Log($"轉發給: {user}");
                            chatRoom.SendNetworkDirectMessage(message, userInfo.IP, userInfo.Port);
                        }
                    }
                }
            }
        }
    }
} 
