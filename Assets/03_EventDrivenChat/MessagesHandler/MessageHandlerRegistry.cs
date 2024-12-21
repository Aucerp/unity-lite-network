using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unet.EventDrivenChat.MessagesHandler
{
    public class MessageHandlerRegistry
    {
        private readonly Dictionary<MessageType, IMessageHandler> handlers = 
            new Dictionary<MessageType, IMessageHandler>();

        public void RegisterHandler(IMessageHandler handler)
        {
            handlers[handler.HandledMessageType] = handler;
            Debug.Log($"註冊消息處理器: {handler.HandledMessageType}");
        }

        public void HandleMessage(ChatMessage message)
        {
            if (handlers.TryGetValue(message.Type, out var handler))
            {
                try
                {
                    handler.HandleMessage(message);
                }
                catch (Exception ex)
                {
                    ChatEvents.RaiseError($"處理消息時發生錯誤: {ex.Message}");
                }
            }
            else
            {
                ChatEvents.RaiseError($"找不到消息類型 {message.Type} 的處理器");
            }
        }
    }
} 