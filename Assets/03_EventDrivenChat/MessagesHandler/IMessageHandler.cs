namespace Unet.EventDrivenChat.MessagesHandler
{
    public interface IMessageHandler
    {
        MessageType HandledMessageType { get; }
        void HandleMessage(ChatMessage message);
    }
} 