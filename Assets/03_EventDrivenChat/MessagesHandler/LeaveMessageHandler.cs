namespace Unet.EventDrivenChat.MessagesHandler
{
    public class LeaveMessageHandler : BaseMessageHandler
    {
        public override MessageType HandledMessageType => MessageType.Leave;

        public LeaveMessageHandler(ChatRoom room) : base(room) { }

        public override void HandleMessage(ChatMessage message)
        {
            LogHandling(message);

            chatRoom.RemoveUser(message.FromName);

            if (message.FromName == chatRoom.HostName || message.Content == "主機關閉房間")
            {
                ChatEvents.RaiseSystemMessage("主機關閉房間");
                chatRoom.LeaveRoom();
            }
            else
            {
                ChatEvents.RaiseSystemMessage($"{message.FromName} 離開了聊天室");
            }
        }
    }
} 