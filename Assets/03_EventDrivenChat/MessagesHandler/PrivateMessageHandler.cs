namespace Unet.EventDrivenChat.MessagesHandler
{
    public class PrivateMessageHandler : BaseMessageHandler
    {
        public override MessageType HandledMessageType => MessageType.Private;

        public PrivateMessageHandler(ChatRoom room) : base(room) { }

        public override void HandleMessage(ChatMessage message)
        {
            LogHandling(message);
            if (message.ToName == chatRoom.UserName || message.FromName == chatRoom.UserName)
            {
                ChatEvents.RaiseMessageReceived(message);
            }
        }
    }
} 