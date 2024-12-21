using UnityEngine;
using System;

namespace Unet.EventDrivenChat
{
    public class ChatMonoEntry : MonoBehaviour
    {
        public ChatRoom ChatRoom { get; private set; }
        private ChatRoomUIFactory uiFactory;

        void Awake()
        {
            try
            {
                var dispatcher = UnityMainThreadDispatcher.Instance;
                ChatRoom = new ChatRoom();
                uiFactory = gameObject.AddComponent<ChatRoomUIFactory>();
                uiFactory.Init(ChatRoom);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatMonoEntry] 初始化錯誤: {ex.Message}\n{ex.StackTrace}");
            }
        }

        void OnDestroy()
        {
            ChatRoom?.Dispose();
        }
    }
} 