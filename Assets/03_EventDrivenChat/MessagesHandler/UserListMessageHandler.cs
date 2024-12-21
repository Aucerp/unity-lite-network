using System;
using System.Linq;
using UnityEngine;

namespace Unet.EventDrivenChat.MessagesHandler
{
    public class UserListMessageHandler : BaseMessageHandler
    {
        public override MessageType HandledMessageType => MessageType.UserList;

        public UserListMessageHandler(ChatRoom room) : base(room) { }

        public override void HandleMessage(ChatMessage message)
        {
            LogHandling(message);
            
            if (!chatRoom.IsHost)
            {
                var users = message.Content.Split(',');
                foreach (var user in users)
                {
                    if (!string.IsNullOrEmpty(user) && user != chatRoom.UserName)
                    {
                        chatRoom.AddUser(user, message.FromIP, message.FromPort);
                    }
                }
                ChatEvents.RaiseUserListUpdated(chatRoom.GetUserList());
                ChatEvents.RaiseSystemMessage($"用戶列表已更新，當前在線人數: {chatRoom.GetUserList().Count}");
            }
        }
    }
} 