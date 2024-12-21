namespace Unet.EventDrivenChat.MessagesHandler
{
    public class SystemMessageHandler : BaseMessageHandler
    {
        public override MessageType HandledMessageType => MessageType.System;

        public SystemMessageHandler(ChatRoom room) : base(room) { }

        public override void HandleMessage(ChatMessage message)
        {
            LogHandling(message);
            ChatEvents.RaiseSystemMessage(message.Content);
        }
    }
} 