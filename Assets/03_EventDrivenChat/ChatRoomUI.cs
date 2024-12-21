using UnityEngine;
using System.Collections.Generic;
using System;

namespace Unet.EventDrivenChat
{
    public class ChatRoomUI : MonoBehaviour
    {
        #region 欄位定義
        private ChatRoom chatRoom;
        private ChatRoomUILayout layout;
        private Vector2 scrollPosition;
        private Vector2 userListScrollPosition;
        private string inputMessage = string.Empty;
        private string selectedUser = string.Empty;
        private List<string> messageLog = new List<string>();
        private List<string> userList = new List<string>();

        // 連接設置
        private string userName = "P1";
        private string hostIP = "127.0.0.1";
        private int hostPort = 11000;
        private bool isInRoom = false;

        // 用戶名稱計數器
        private static int userCounter = 1;

        private const int MAX_LOG_COUNT = 100;

        // 添加錯誤提示相關欄位
        private string errorMessage = string.Empty;
        private float errorMessageTimer = 0f;
        private const float ERROR_MESSAGE_DURATION = 1f;
        #endregion

        #region 顏色設定
        // 用戶顏色字典
        private Dictionary<string, Color> userColors = new Dictionary<string, Color>();
        private readonly Color[] colorPool = new Color[] 
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
        private static readonly Color HOST_COLOR = Color.yellow;                    // 房主顏色
        private static readonly Color PRIVATE_CHAT_COLOR = new Color(0.5f, 0, 0.5f); // 私聊狀態顏色
        private static readonly Color SELECTED_COLOR = Color.cyan;                  // 選中狀態顏色
        #endregion

        #region Unity 生命週期
        void Awake()
        {
            try
            {
                Debug.Log("[ChatRoomUI] Awake 開始");
                layout = new ChatRoomUILayout();
                Debug.Log("[ChatRoomUI] 布局已初始化");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatRoomUI] Awake 錯誤: {ex.Message}");
            }
        }

        void OnGUI()
        {
            // 移除房間狀���顯示
            // GUI.Label(layout.GetStatusLabelRect(), $"房間狀態: {(isInRoom ? "在房間中" : "未在房間中")}");
            
            if (!isInRoom)
            {
                DrawLoginWindow();
            }
            else
            {
                DrawChatWindow();
            }
        }

        void Update()
        {
            // 更新錯誤消息計時器
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
            try
            {
                Debug.Log("[ChatRoomUI] 開始清理");
                UnsubscribeFromEvents();
                Debug.Log("[ChatRoomUI] 清理完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatRoomUI] 清理時發生錯誤: {ex.Message}");
            }
        }
        #endregion

        #region 初始化
        public void Initialize(ChatRoom room)
        {
            try 
            {
                Debug.Log("[ChatRoomUI] 開始初始化");
                chatRoom = room;
                
                // 生成用戶名稱 P1-P100
                userName = $"P{userCounter}";
                userCounter = (userCounter % 100) + 1;  // 循環使用 1-100
                
                SubscribeToEvents();
                Debug.Log("[ChatRoomUI] 初始化完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatRoomUI] 初始化錯誤: {ex.Message}");
            }
        }
        #endregion

        #region 事件管理
        private void SubscribeToEvents()
        {
            try
            {
                Debug.Log("[ChatRoomUI] 開始訂閱事件");
                
                // 直接訂閱事件
                ChatEvents.OnMessageReceived += HandleMessageReceived;
                ChatEvents.OnUserListUpdated += HandleUserListUpdated;
                ChatEvents.OnRoomStateChanged += HandleRoomStateChanged;
                ChatEvents.OnError += HandleError;
                ChatEvents.OnSystemMessage += HandleSystemMessage;
                
                Debug.Log("[ChatRoomUI] 事件訂閱完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatRoomUI] 訂閱事件時發生錯誤: {ex.Message}");
            }
        }

        private void UnsubscribeFromEvents()
        {
            try
            {
                ChatEvents.OnMessageReceived -= HandleMessageReceived;
                ChatEvents.OnUserListUpdated -= HandleUserListUpdated;
                ChatEvents.OnRoomStateChanged -= HandleRoomStateChanged;
                ChatEvents.OnError -= HandleError;
                ChatEvents.OnSystemMessage -= HandleSystemMessage;
                Debug.Log("[ChatRoomUI] 已取消所有事件��閱");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatRoomUI] 取消訂閱時發生錯誤: {ex.Message}");
            }
        }
        #endregion

        #region 介面繪制
        private void DrawLoginWindow()
        {
            GUILayout.Window(1, layout.LoginWindowRect, DrawLoginWindowContent, "聊天室登入");
        }

        private void DrawChatWindow()
        {
            GUILayout.Window(0, layout.ChatWindowRect, DrawWindow, GetWindowTitle());
        }

        private void DrawWindow(int windowID)
        {
            try
            {
                // 右上角關閉按鈕
                GUI.backgroundColor = Color.red;
                if (GUI.Button(layout.CloseButtonRect, "✕"))
                {
                    GUI.enabled = false;
                    chatRoom.LeaveRoom();
                    GUI.enabled = true;
                }
                GUI.backgroundColor = Color.white;

                if (layout.IsPortrait)
                {
                    // 縱向模式佈局
                    GUILayout.BeginVertical(); // 主要容器

                    // 1. 水平用戶列表
                    GUILayout.BeginHorizontal(GUILayout.Height(layout.UserListHeight));
                    userListScrollPosition = GUILayout.BeginScrollView(
                        userListScrollPosition, 
                        GUILayout.Height(layout.UserListHeight),
                        GUILayout.Width(layout.WindowWidth - ChatRoomUILayout.MARGIN * 2)
                    );

                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < userList.Count; i++)
                    {
                        string user = userList[i];
                        bool isSelected = user == selectedUser;
                        bool isSelf = user == chatRoom.UserName;
                        bool isHost = user == chatRoom.HostName;

                        if (isHost) GUI.color = HOST_COLOR;
                        else if (isSelected) GUI.color = PRIVATE_CHAT_COLOR;
                        else GUI.color = AssignUserColor(user);

                        string displayName = user;
                        if (isSelf) displayName = "*" + displayName;
                        if (isHost) displayName += "★";

                        // 計按鈕寬度：最小適應文字寬度，最大8個字符寬度
                        float charWidth = layout.GetCurrentButtonStyle().CalcSize(new GUIContent("A")).x;
                        float minWidth = layout.GetCurrentButtonStyle().CalcSize(new GUIContent(displayName)).x + 10;
                        float maxWidth = charWidth * 8;
                        float buttonWidth = Mathf.Min(maxWidth, Mathf.Max(minWidth, layout.UserButtonWidth));

                        // 使用計算出的寬度
                        if (GUILayout.Button(displayName, layout.GetCurrentButtonStyle(), 
                            GUILayout.Width(buttonWidth),
                            GUILayout.Height(layout.UserListHeight - ChatRoomUILayout.MARGIN * 2)))
                        {
                            selectedUser = (isSelf || isSelected) ? string.Empty : user;
                        }
                        
                        // 減少按鈕間的間距
                        GUILayout.Space(ChatRoomUILayout.INNER_MARGIN/2);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndScrollView();
                    
                    // 私聊狀態
                    if (!string.IsNullOrEmpty(selectedUser))
                    {
                        GUI.color = PRIVATE_CHAT_COLOR;
                        GUILayout.Label($"→{selectedUser}", layout.GetCurrentChatStyle(), GUILayout.Width(80));
                    }
                    GUILayout.EndHorizontal();

                    // 2. 聊天區域 - 使用 GUILayout.BeginArea 隔離
                    Color originalColor = GUI.color;  // 保存原始顏色
                    
                    // 計算聊天區域的位置大小
                    Rect chatAreaRect = layout.GetChatAreaRect();  // 直接使用 layout 提供的方法

                    GUILayout.BeginArea(chatAreaRect);
                    {
                        GUI.color = Color.white;  // 重置顏色為白色
                        
                        scrollPosition = GUILayout.BeginScrollView(
                            scrollPosition,
                            GUILayout.Width(layout.ChatAreaWidth),
                            GUILayout.Height(layout.ChatAreaHeight)
                        );

                        foreach (string message in messageLog)
                        {
                            GUILayout.Label(message, layout.GetCurrentChatStyle());
                        }
                        
                        GUILayout.EndScrollView();
                    }
                    GUILayout.EndArea();
                    
                    GUI.color = originalColor;  // 恢復原始顏色

                    // 3. 輸入區域 - 使用 BeginArea 隔離
                    Color originalInputColor = GUI.color;
                    GUI.color = Color.white;  // 重置顏色

                    Rect inputAreaRect = new Rect(
                        ChatRoomUILayout.MARGIN,
                        layout.WindowHeight - layout.InputAreaHeight - ChatRoomUILayout.MARGIN,
                        layout.WindowWidth - (ChatRoomUILayout.MARGIN * 2),
                        layout.InputAreaHeight
                    );

                    GUILayout.BeginArea(inputAreaRect);
                    {
                        GUILayout.BeginHorizontal();
                        
                        // 輸入框
                        GUI.SetNextControlName("ChatInput");
                        inputMessage = GUILayout.TextField(
                            inputMessage, 
                            layout.GetCurrentInputStyle(), 
                            GUILayout.Width(layout.InputFieldWidth),
                            GUILayout.Height(layout.InputAreaHeight - ChatRoomUILayout.MARGIN)
                        );

                        // 發送按鈕 - 使用專用樣式
                        if (GUILayout.Button("發送", 
                            layout.GetSendButtonStyle(),  // 使用新的發送按鈕樣式
                            GUILayout.Width(layout.SendButtonWidth),
                            GUILayout.Height(layout.InputAreaHeight - ChatRoomUILayout.MARGIN)))
                        {
                            if (!string.IsNullOrEmpty(inputMessage))
                            {
                                SendChatMessageToRoom();
                            }
                        }

                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndArea();

                    GUI.color = originalInputColor;  // 恢復原始顏色

                    GUILayout.EndVertical(); // 結束主要容器
                }
                else
                {
                    // 橫向模式保持原有的佈局
                    // ... 原有的橫向模式代碼 ...
                }

                // 保持輸入框焦點
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.FocusControl("ChatInput");
                }

                GUI.color = Color.white; // 重置顏色
            }
            catch (Exception ex)
            {
                Debug.LogError($"繪製聊天窗口時發生錯誤: {ex}");
            }
        }

        private void DrawLoginWindowContent(int windowID)
        {
            GUILayout.Space(20);
            GUILayout.BeginVertical();

            // 用戶名稱輸入
            GUILayout.Label("用戶名稱:");
            userName = GUILayout.TextField(userName);

            GUILayout.Space(10);

            // 主機設置區域
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("建立房間:");
            
            // 主機端口設置
            GUILayout.BeginHorizontal();
            GUILayout.Label("主機端口:", GUILayout.Width(60));
            string hostPortStr = GUILayout.TextField(hostPort.ToString(), GUILayout.Width(60));
            int.TryParse(hostPortStr, out hostPort);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            
            // 建立房間按鈕（高度加倍）
            if (GUILayout.Button("建立房間", GUILayout.Height(40)))
            {
                chatRoom.CreateRoom(userName, hostPort);
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // 客戶端設置區域
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("加入現有房間:");

            // IP 輸入
            GUILayout.BeginHorizontal();
            GUILayout.Label("主機 IP:", GUILayout.Width(60));
            hostIP = GUILayout.TextField(hostIP, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            // 端口輸入
            GUILayout.BeginHorizontal();
            GUILayout.Label("連接埠:", GUILayout.Width(60));
            string portStr = GUILayout.TextField(hostPort.ToString(), GUILayout.Width(60));
            int.TryParse(portStr, out hostPort);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            
            // 加入房間按鈕（高度加倍）
            if (GUILayout.Button("加入房間", GUILayout.Height(40)))
            {
                int randomPort = UnityEngine.Random.Range(11001, 12000);
                chatRoom.JoinRoom(userName, randomPort, hostIP, hostPort);
            }

            // 顯示錯誤消息
            if (!string.IsNullOrEmpty(errorMessage))
            {
                GUILayout.Space(5);
                var style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.red;
                style.fontSize = 14;
                style.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label(errorMessage, style);
            }

            GUILayout.EndVertical();

            GUILayout.EndVertical();
        }
        #endregion

        #region 訊息處理
        private void HandleMessageReceived(ChatMessage message)
        {
            try 
            {
                Debug.Log($"[HandleMessageReceived] 開始處理消息");
                
                // 獲取發送者的顏色
                Color senderColor;
                if (message.FromName == chatRoom.HostName)
                {
                    senderColor = HOST_COLOR;
                }
                else
                {
                    senderColor = AssignUserColor(message.FromName);
                }
                
                string colorHex = ColorUtility.ToHtmlStringRGB(senderColor);
                string privateColorHex = ColorUtility.ToHtmlStringRGB(PRIVATE_CHAT_COLOR);
                
                // 根據模式決定消息格式
                string log;
                if (layout.IsPortrait)
                {
                    // 縱向式：不顯示時間
                    string prefix = "";
                    if (message.Type == MessageType.Private)
                    {
                        if (message.FromName == chatRoom.UserName)
                        {
                            prefix = $"<color=#{privateColorHex}>[私>{message.ToName}]</color>";
                        }
                        else
                        {
                            prefix = $"<color=#{privateColorHex}>[{message.FromName}>我]</color>";
                        }
                    }
                    log = $"{prefix}<color=#{colorHex}>{message.FromName}</color>: {message.Content}";
                }
                else
                {
                    // 橫向模式：保持原有格式
                    string prefix = "";
                    if (message.Type == MessageType.Private)
                    {
                        if (message.FromName == chatRoom.UserName)
                        {
                            prefix = $"<color=#{privateColorHex}>[私聊給 {message.ToName}]</color>";
                        }
                        else
                        {
                            prefix = $"<color=#{privateColorHex}>[來自 {message.FromName} 的私聊]</color>";
                        }
                    }
                    log = $"[{DateTime.Now:HH:mm:ss}] {prefix}<color=#{colorHex}>{message.FromName}</color>: {message.Content}";
                }

                Debug.Log($"[HandleMessageReceived] 準備添加日誌: {log}");
                
                UnityMainThreadDispatcher.Instance.Enqueue(() => 
                {
                    if (!messageLog.Contains(log))
                    {
                        messageLog.Add(log);
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
                Debug.LogError($"[HandleMessageReceived] 處理消息時發生錯誤: {ex.Message}");
            }
        }

        private void HandleUserListUpdated(List<string> users)
        {
            userList = new List<string>(users);
            
            // 清除舊的顏色分配
            userColors.Clear();
            
            // 為所有戶重新分配顏色
            foreach (string user in users)
            {
                AssignUserColor(user);
            }

            // 檢查選中的用戶是否還在列表中
            if (!string.IsNullOrEmpty(selectedUser) && !users.Contains(selectedUser))
            {
                selectedUser = string.Empty;
            }
        }

        private void HandleRoomStateChanged(bool connected)
        {
            isInRoom = connected;
            if (!connected)
            {
                messageLog.Clear();
                userList.Clear();
                selectedUser = string.Empty;
                userColors.Clear();  // 清除顏色分配
            }
        }

        private void HandleError(string error)
        {
            // 所有錯誤都使用紅色顯示
            ShowErrorMessage(error);
        }

        private void HandleSystemMessage(string message)
        {
            AddToLog($"系統: {message}");
        }
        #endregion

        #region 顏色管理
        private Color AssignUserColor(string userName)
        {
            if (!userColors.ContainsKey(userName))
            {
                // 使用用戶名的哈希值來確定顏色索引，這樣同一個用戶名總會得到相同的顏色
                int colorIndex = Math.Abs(userName.GetHashCode()) % colorPool.Length;
                userColors[userName] = colorPool[colorIndex];
            }
            return userColors[userName];
        }
        #endregion

        #region 工具方法
        private string GetWindowTitle()
        {
            string baseTitle = chatRoom.IsHost ? 
                $"聊天室 - [Host] {GetLocalIPAddress()}:{chatRoom.LocalPort}" :
                $"聊天室 - [Client] {hostIP}:{hostPort}";

            if (!string.IsNullOrEmpty(selectedUser))
            {
                return $"{baseTitle} [私聊中: {selectedUser}]";
            }
            return baseTitle;
        }

        private string GetLocalIPAddress()
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

        private void AddToLog(string message)
        {
            try 
            {
                Debug.Log($"添加日誌: {message}");
                
                // 使用 UnityMainThreadDispatcher 確保在主線程更新 UI
                UnityMainThreadDispatcher.Instance.Enqueue(() => 
                {
                    if (!messageLog.Contains(message))  // 避免重複消息
                    {
                        messageLog.Add(message);
                        if (messageLog.Count > MAX_LOG_COUNT)
                        {
                            messageLog.RemoveAt(0);
                        }
                        // ���動滾動到底部
                        scrollPosition.y = float.MaxValue;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"添加日誌時發生錯誤: {ex.Message}");
            }
        }

        private void SendChatMessageToRoom()
        {
            if (string.IsNullOrEmpty(inputMessage)) return;

            Debug.Log($"[SendChatMessageToRoom] 開始發送消息");
            Debug.Log($"[SendChatMessageToRoom] 消息內容: {inputMessage}");
            Debug.Log($"[SendChatMessageToRoom] 選擇的用戶: {selectedUser}");
            Debug.Log($"[SendChatMessageToRoom] 當前用戶: {chatRoom.UserName}");

            // 檢查是否是私聊
            if (!string.IsNullOrEmpty(selectedUser))
            {
                if (selectedUser == chatRoom.UserName)
                {
                    AddToLog("錯誤: 不能私聊自己");
                    return;
                }
                Debug.Log($"[SendChatMessageToRoom] 發送私聊消息給: {selectedUser}");
            }
            else
            {
                Debug.Log("[SendChatMessageToRoom] 發送公共消息");
            }

            try 
            {
                // 只有在選擇了用戶時才發送私聊消息
                string targetUser = string.IsNullOrEmpty(selectedUser) ? null : selectedUser;
                chatRoom.SendChatMessage(inputMessage, targetUser);
                inputMessage = string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SendChatMessageToRoom] 發送消息時發生錯誤: {ex.Message}");
                AddToLog($"發送消息失敗: {ex.Message}");
            }
        }

        private void ShowErrorMessage(string message)
        {
            Debug.LogError($"錯誤: {message}");  // 同時在控制台顯示錯誤
            errorMessage = message;
            errorMessageTimer = ERROR_MESSAGE_DURATION;
        }
        #endregion
    }
} 
