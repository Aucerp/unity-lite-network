using System;

namespace Unet.LitUdp.Chat
{
    [Serializable]
    public class ChatMessage
    {
        public string FromName;
        public string ToName;    // 如果是空字符串，則為廣播消息
        public string Content;
        public MessageType Type;
        public long Timestamp;
        public int FromPort;

        public ChatMessage()
        {
            Timestamp = DateTime.Now.Ticks;
        }
    }

    [Serializable]
    public enum MessageType
    {
        Chat,           // 普通聊天消息
        Join,           // 加入聊天室
        Leave,          // 離開聊天室
        UserList,       // 用戶列表更新
        Private         // 私聊消息
    }
} 