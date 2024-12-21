using UnityEngine;

namespace Unet.EventDrivenChat.MessagesHandler
{
    public abstract class BaseMessageHandler : IMessageHandler
    {
        protected readonly ChatRoom chatRoom;
        public abstract MessageType HandledMessageType { get; }

        protected BaseMessageHandler(ChatRoom room)
        {
            chatRoom = room;
        }

        public abstract void HandleMessage(ChatMessage message);

        protected virtual void LogHandling(ChatMessage message)
        {
            string rolePrefix = chatRoom.IsHost ? "服務端" : "客戶端";
            string channelType = message.Type == MessageType.Private ? "[Private]" : "[Public]";
            string endpoint = $"{message.FromName}@{message.FromPort}";
            
            string formattedMessage = $"[{rolePrefix}:{endpoint}]{channelType} {message.Content}";
            Debug.Log(formattedMessage);
        }
    }
} 