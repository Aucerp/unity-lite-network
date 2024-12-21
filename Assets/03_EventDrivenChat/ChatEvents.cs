using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unet.EventDrivenChat
{
    public static class ChatEvents
    {
        // 核心事件
        public static event Action<ChatMessage> OnMessageReceived;
        public static event Action<List<string>> OnUserListUpdated;
        public static event Action<bool> OnRoomStateChanged;
        public static event Action<string> OnError;
        public static event Action<string> OnSystemMessage;

        private static readonly object _lock = new object();

        // 事件觸發方法
        public static void RaiseMessageReceived(ChatMessage message)
        {
            if (message == null)
            {
                Debug.LogError("嘗試觸發空消息事件");
                return;
            }

            try
            {
                Debug.Log($"[ChatEvents] 觸發消息接收事件: Type={message.Type}, From={message.FromName}, Content={message.Content}");
                lock (_lock)
                {
                    if (OnMessageReceived != null)
                    {
                        OnMessageReceived.Invoke(message);
                    }
                    else
                    {
                        Debug.LogWarning("[ChatEvents] 沒有訂閱者處理消息接收事件");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatEvents] 處理消息事件時發生錯誤: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static void RaiseUserListUpdated(List<string> users)
        {
            if (users == null)
            {
                Debug.LogError("嘗試觸發空用戶列表事件");
                return;
            }

            try
            {
                Debug.Log($"觸發用戶列表更新事件: 用戶數={users.Count}");
                lock (_lock)
                {
                    OnUserListUpdated?.Invoke(users);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"處理用戶列表更新時發生錯誤: {ex}");
            }
        }

        public static void RaiseRoomStateChanged(bool isConnected)
        {
            try
            {
                Debug.Log($"觸發房間狀態變更事件: isConnected={isConnected}");
                lock (_lock)
                {
                    OnRoomStateChanged?.Invoke(isConnected);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"處理房間狀態變更時發生錯誤: {ex}");
            }
        }

        public static void RaiseError(string error)
        {
            if (string.IsNullOrEmpty(error))
            {
                Debug.LogError("嘗試觸發空錯誤事件");
                return;
            }

            try
            {
                Debug.LogError($"聊天室錯誤: {error}");
                lock (_lock)
                {
                    OnError?.Invoke(error);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"處理錯誤事件時發生錯誤: {ex}");
            }
        }

        public static void RaiseSystemMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogError("嘗試觸發空系統消息事件");
                return;
            }

            try
            {
                Debug.Log($"系統消息: {message}");
                lock (_lock)
                {
                    OnSystemMessage?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"處理系統消息時發生錯誤: {ex}");
            }
        }
    }
} 