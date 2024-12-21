using System;
using UnityEngine;

namespace Unet.EventDrivenChat
{
    [Serializable]
    public class ChatMessage
    {
        public string FromName;
        public string ToName;
        public string Content;
        public MessageType Type;
        public long Timestamp;
        public int FromPort;
        public string FromIP;

        public ChatMessage()
        {
            Timestamp = DateTime.Now.Ticks;
        }
    }

    [Serializable]
    public enum MessageType
    {
        Chat,       // 普通聊天消息
        Join,       // 加入聊天室
        Leave,      // 離開聊天室
        UserList,   // 用戶列表更新
        Private,    // 私聊消息
        System      // 系統消息
    }
} 