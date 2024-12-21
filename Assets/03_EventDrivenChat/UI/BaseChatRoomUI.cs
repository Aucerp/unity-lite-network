using UnityEngine;
using System.Collections.Generic;
using System;

namespace Unet.EventDrivenChat
{
    public abstract class BaseChatRoomUI : MonoBehaviour
    {
        #region 共用欄位
        protected ChatRoom chatRoom;
        protected Vector2 scrollPosition;
        protected Vector2 userListScrollPosition;
        protected string inputMessage = string.Empty;
        protected string selectedUser = string.Empty;
        protected List<string> messageLog = new List<string>();
        protected List<string> userList = new List<string>();

        // 連接設置
        protected string userName = "P1";
        protected string hostIP = "127.0.0.1";
        protected int hostPort = 11000;
        protected bool isInRoom = false;

        // 用戶名稱計數器
        protected static int userCounter = 1;

        protected const int MAX_LOG_COUNT = 100;

        // 錯誤提示相關欄位
        protected string errorMessage = string.Empty;
        protected float errorMessageTimer = 0f;
        protected const float ERROR_MESSAGE_DURATION = 1f;

        // 顏色設定
        protected Dictionary<string, Color> userColors = new Dictionary<string, Color>();
        protected readonly Color[] colorPool = new Color[] 
        {
            Color.red,
            new Color(1f, 0.5f, 0f),  // 橙色
            Color.green,
            Color.cyan,
            Color.magenta,
            new Color(1f, 1f, 0.5f),  // 淺黃
            new Color(0.5f, 1f, 0.5f), // 淺綠
            new Color(0.5f, 0.5f, 1f)  // 淺藍
        };

        // 固定顏色定義
        protected static readonly Color HOST_COLOR = Color.yellow;
        protected static readonly Color PRIVATE_CHAT_COLOR = new Color(0.5f, 0, 0.5f);
        protected static readonly Color SELECTED_COLOR = Color.cyan;
        #endregion

        #region 抽象方法
        protected abstract void DrawLoginWindow();
        protected abstract void DrawChatWindow();
        protected abstract void DrawLoginWindowContent(int windowID);
        protected abstract void DrawWindow(int windowID);
        #endregion

        #region 共用方法
        public void Initialize(ChatRoom room)
        {
            try 
            {
                Debug.Log("[ChatRoomUI] 開始初始化");
                chatRoom = room;
                
                userName = $"P{userCounter}";
                userCounter = (userCounter % 100) + 1;
                
                SubscribeToEvents();
                Debug.Log("[ChatRoomUI] 初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatRoomUI] 初始化錯誤: {ex.Message}");
            }
        }

        protected void SubscribeToEvents()
        {
            ChatEvents.OnMessageReceived += HandleMessageReceived;
            ChatEvents.OnUserListUpdated += HandleUserListUpdated;
            ChatEvents.OnRoomStateChanged += HandleRoomStateChanged;
            ChatEvents.OnError += HandleError;
            ChatEvents.OnSystemMessage += HandleSystemMessage;
        }

        protected void UnsubscribeFromEvents()
        {
            ChatEvents.OnMessageReceived -= HandleMessageReceived;
            ChatEvents.OnUserListUpdated -= HandleUserListUpdated;
            ChatEvents.OnRoomStateChanged -= HandleRoomStateChanged;
            ChatEvents.OnError -= HandleError;
            ChatEvents.OnSystemMessage -= HandleSystemMessage;
        }

        protected void HandleMessageReceived(ChatMessage message)
        {
            try 
            {
                Color senderColor = message.FromName == chatRoom.HostName ? 
                    HOST_COLOR : AssignUserColor(message.FromName);
                
                string colorHex = ColorUtility.ToHtmlStringRGB(senderColor);
                string privateColorHex = ColorUtility.ToHtmlStringRGB(PRIVATE_CHAT_COLOR);
                
                string log = FormatMessage(message, colorHex, privateColorHex);
                AddToLog(log);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandleMessageReceived] 處理消息時發生錯誤: {ex.Message}");
            }
        }

        protected abstract string FormatMessage(ChatMessage message, string colorHex, string privateColorHex);

        protected void HandleUserListUpdated(List<string> users)
        {
            userList = new List<string>(users);
            userColors.Clear();
            foreach (string user in users)
            {
                AssignUserColor(user);
            }
            if (!string.IsNullOrEmpty(selectedUser) && !users.Contains(selectedUser))
            {
                selectedUser = string.Empty;
            }
        }

        protected void HandleRoomStateChanged(bool connected)
        {
            isInRoom = connected;
            if (!connected)
            {
                messageLog.Clear();
                userList.Clear();
                selectedUser = string.Empty;
                userColors.Clear();
            }
        }

        protected void HandleError(string error)
        {
            ShowErrorMessage(error);
        }

        protected void HandleSystemMessage(string message)
        {
            AddToLog($"系統: {message}");
        }

        protected Color AssignUserColor(string userName)
        {
            if (!userColors.ContainsKey(userName))
            {
                int colorIndex = Math.Abs(userName.GetHashCode()) % colorPool.Length;
                userColors[userName] = colorPool[colorIndex];
            }
            return userColors[userName];
        }

        protected string GetWindowTitle()
        {
            string baseTitle = chatRoom.IsHost ? 
                $"聊天室 - [Host] {GetLocalIPAddress()}:{chatRoom.LocalPort}" :
                $"聊天室 - [Client] {hostIP}:{hostPort}";

            return !string.IsNullOrEmpty(selectedUser) ? 
                $"{baseTitle} [私聊中: {selectedUser}]" : baseTitle;
        }

        protected string GetLocalIPAddress()
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }

        protected void AddToLog(string message)
        {
            try 
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() => 
                {
                    if (!messageLog.Contains(message))
                    {
                        messageLog.Add(message);
                        if (messageLog.Count > MAX_LOG_COUNT)
                        {
                            messageLog.RemoveAt(0);
                        }
                        scrollPosition.y = float.MaxValue;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"添加日誌時發生錯誤: {ex.Message}");
            }
        }

        protected void SendChatMessageToRoom()
        {
            if (string.IsNullOrEmpty(inputMessage)) return;

            try 
            {
                string targetUser = string.IsNullOrEmpty(selectedUser) ? null : selectedUser;
                if (!string.IsNullOrEmpty(targetUser) && targetUser == chatRoom.UserName)
                {
                    AddToLog("錯誤: 不能私聊自己");
                    return;
                }
                
                chatRoom.SendChatMessage(inputMessage, targetUser);
                inputMessage = string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SendChatMessageToRoom] 發送消息時發生錯誤: {ex.Message}");
                AddToLog($"發送消息失敗: {ex.Message}");
            }
        }

        protected void ShowErrorMessage(string message)
        {
            Debug.LogError($"錯誤: {message}");
            errorMessage = message;
            errorMessageTimer = ERROR_MESSAGE_DURATION;
        }

        protected void UpdateErrorMessageTimer()
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                errorMessageTimer -= Time.deltaTime;
                if (errorMessageTimer <= 0)
                {
                    errorMessage = string.Empty;
                }
            }
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        void Update()
        {
            UpdateErrorMessageTimer();
        }

        void OnGUI()
        {
            if (!isInRoom)
            {
                DrawLoginWindow();
            }
            else
            {
                DrawChatWindow();
            }
        }
        #endregion
    }
} 