using UnityEngine;
using System;

namespace Unet.EventDrivenChat
{
    /// <summary>
    /// 聊天室程序的入口點，負責初始化和管理聊天室核心組件
    /// </summary>
    public class ChatMonoEntry : MonoBehaviour
    {
        #region 私有欄位
        private ChatRoom _chatRoom;
        private ChatRoomUI _chatRoomUI;
        #endregion

        #region Unity 生命週期
        private void Awake()
        {
            try
            {
                InitializeComponents();
            }
            catch (Exception ex)
            {
                HandleInitializationError(ex);
            }
        }

        private void OnDestroy()
        {
            try
            {
                CleanupComponents();
            }
            catch (Exception ex)
            {
                HandleCleanupError(ex);
            }
        }
        #endregion

        #region 初始化相關
        private void InitializeComponents()
        {
            LogInitializationStart();
            
            EnsureDispatcherExists();
            InitializeChatRoom();
            InitializeChatRoomUI();
            
            LogInitializationComplete();
        }

        private void EnsureDispatcherExists()
        {
            var dispatcher = UnityMainThreadDispatcher.Instance;
            Debug.Log("[ChatMonoEntry] UnityMainThreadDispatcher 已確認");
        }

        private void InitializeChatRoom()
        {
            _chatRoom = new ChatRoom();
            Debug.Log("[ChatMonoEntry] ChatRoom 已創建");
        }

        private void InitializeChatRoomUI()
        {
            _chatRoomUI = gameObject.AddComponent<ChatRoomUI>();
            Debug.Log("[ChatMonoEntry] ChatRoomUI 組件已添加");

            UnityMainThreadDispatcher.Instance.Enqueue(() => 
            {
                _chatRoomUI.Initialize(_chatRoom);
                Debug.Log("[ChatMonoEntry] ChatRoomUI 已初始化");
            });
        }
        #endregion

        #region 清理相關
        private void CleanupComponents()
        {
            Debug.Log("[ChatMonoEntry] 開始清理組件");
            
            if (_chatRoom != null)
            {
                _chatRoom.Dispose();
                _chatRoom = null;
                Debug.Log("[ChatMonoEntry] ChatRoom 已清理");
            }
        }
        #endregion

        #region 錯誤處理
        private void HandleInitializationError(Exception ex)
        {
            string errorMessage = $"[ChatMonoEntry] 初始化時發生錯誤: {ex.Message}";
            Debug.LogError($"{errorMessage}\n{ex.StackTrace}");
            
            // 可以在這裡添加額外的錯誤處理邏輯
            // 例如：顯示錯誤對話框、重試機制等
        }

        private void HandleCleanupError(Exception ex)
        {
            string errorMessage = $"[ChatMonoEntry] 清理時發生錯誤: {ex.Message}";
            Debug.LogError($"{errorMessage}\n{ex.StackTrace}");
        }
        #endregion

        #region 日誌相關
        private void LogInitializationStart()
        {
            Debug.Log("[ChatMonoEntry] 開始初始化");
        }

        private void LogInitializationComplete()
        {
            Debug.Log("[ChatMonoEntry] 初始化完成");
        }
        #endregion
    }
} 