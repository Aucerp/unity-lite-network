using UnityEngine;

namespace Unet.EventDrivenChat
{
    public class ChatRoomUIFactory : MonoBehaviour
    {
        private BaseChatRoomUI currentUI;
        private bool isPortrait;
        private ChatRoom chatRoom;

        public void Init(ChatRoom room)
        {
            chatRoom = room;
            UpdateUI();
        }

        void Update()
        {
            bool newIsPortrait = Screen.height > Screen.width;
            if (newIsPortrait != isPortrait)
            {
                isPortrait = newIsPortrait;
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (chatRoom == null) return;

            if (currentUI != null)
            {
                Destroy(currentUI);
            }

            isPortrait = Screen.height > Screen.width;
            currentUI = gameObject.AddComponent(isPortrait ? 
                typeof(PortraitChatRoomUI) : 
                typeof(LandscapeChatRoomUI)) as BaseChatRoomUI;

            currentUI.Initialize(chatRoom);
        }
    }
} 
