using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.Threading;
using System;

namespace Unet.LitUdp.Chat
{
    public class ChatRoomUI : MonoBehaviour
    {
        private ChatManager chatManager;
        private Vector2 scrollPosition;
        private string inputMessage = string.Empty;
        private string selectedUser = string.Empty;
        private List<string> messageLog = new List<string>();
        private List<string> userList = new List<string>();

        // 連接設置
        private string userName = "User";
        private string hostIP = "127.0.0.1";
        private int hostPort = 11000;
        private bool isInRoom = false;

        private const float WINDOW_WIDTH = 600f;
        private const float WINDOW_HEIGHT = 400f;
        private const int MAX_LOG_COUNT = 100;

        private Vector2 userListScrollPosition;

        void Start()
        {
            // 確保 UnityMainThreadDispatcher 存在
            if (UnityMainThreadDispatcher.Instance == null)
            {
                Debug.LogError("UnityMainThreadDispatcher 初始化失敗");
                return;
            }

            userName = "User_" + UnityEngine.Random.Range(1000, 9999);

            chatManager = gameObject.AddComponent<ChatManager>();
            chatManager.OnChatMessageReceived += HandleChatMessage;
            chatManager.OnUserListUpdated += HandleUserListUpdate;
            chatManager.OnRoomJoined += HandleRoomJoined;
            chatManager.OnError += HandleError;
        }

        void OnGUI()
        {
            // 添加調試信息
            GUI.Label(new Rect(10, 10, 200, 20), $"房間狀態: {(isInRoom ? "在房間中" : "未在房間中")}");
            
            if (!isInRoom)
            {
                DrawLoginWindow();
            }
            else
            {
                DrawChatWindow();
            }
        }

        void DrawLoginWindow()
        {
            GUILayout.Window(1, new Rect(
                Screen.width / 2 - 200,
                Screen.height / 2 - 100,
                400,
                200
            ), DrawLoginWindowContent, "聊天室登錄");
        }

        void DrawLoginWindowContent(int windowID)
        {
            GUILayout.Space(20);
            GUILayout.BeginVertical();

            GUILayout.Label("用戶名:");
            userName = GUILayout.TextField(userName);

            GUILayout.Space(10);

            if (GUILayout.Button("創建房間"))
            {
                int randomPort = UnityEngine.Random.Range(11001, 12000);
                chatManager.CreateRoom(userName, randomPort);
            }

            GUILayout.Space(10);
            GUILayout.Label("或加入現有房間:");

            GUILayout.BeginHorizontal();
            GUILayout.Label("主機 IP:", GUILayout.Width(60));
            hostIP = GUILayout.TextField(hostIP);
            GUILayout.Label("端口:", GUILayout.Width(40));
            string portStr = GUILayout.TextField(hostPort.ToString(), GUILayout.Width(60));
            int.TryParse(portStr, out hostPort);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("加入房間"))
            {
                int randomPort = UnityEngine.Random.Range(11001, 12000);
                chatManager.JoinRoom(userName, randomPort, hostIP, hostPort);
            }

            GUILayout.EndVertical();
            GUILayout.Space(20);
        }

        void DrawChatWindow()
        {
            GUILayout.Window(0, new Rect(
                Screen.width / 2 - WINDOW_WIDTH / 2,
                Screen.height / 2 - WINDOW_HEIGHT / 2,
                WINDOW_WIDTH,
                WINDOW_HEIGHT
            ), DrawWindow, GetWindowTitle());
        }

        string GetWindowTitle()
        {
            string role = chatManager.IsHost ? "主機" : "客戶端";
            return string.Format("聊天室 - {0} ({1}) - 端口: {2}", 
                chatManager.UserName, 
                role,
                chatManager.IsHost ? chatManager.HostPort : "連接到 " + hostPort);
        }

        void DrawWindow(int windowID)
        {
            try
            {
                GUILayout.BeginHorizontal();

                // 左側聊天��域
                DrawChatArea();

                // 右側用戶列表
                DrawUserList();

                GUILayout.EndHorizontal();

                // 自動聚焦到輸入框
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.FocusControl("ChatInput");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"繪製聊天窗口時發生錯誤: {ex}");
            }
        }

        private void DrawChatArea()
        {
            GUILayout.BeginVertical(GUILayout.Width(WINDOW_WIDTH - 150));
            
            // 消息顯示區
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(WINDOW_HEIGHT - 80));
            foreach (string message in messageLog)
            {
                GUILayout.Label(message);
            }
            GUILayout.EndScrollView();

            // 輸入區域
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("ChatInput");
            inputMessage = GUILayout.TextField(inputMessage, GUILayout.Width(WINDOW_WIDTH - 250));
            
            if (GUILayout.Button("發送", GUILayout.Width(60)))
            {
                SendMessage();
            }
            GUILayout.EndHorizontal();

            // 添加離開房間按鈕
            if (GUILayout.Button("離開房間", GUILayout.Width(80)))
            {
                LeaveRoom();
            }

            GUILayout.EndVertical();
        }

        private void DrawUserList()
        {
            try
            {
                GUILayout.BeginVertical(GUILayout.Width(130));
                
                // 顯示用戶數量
                string userCountText = $"在線用戶 ({(userList != null ? userList.Count : 0)})";
                GUILayout.Label(userCountText);
                
                // 使用 ScrollView 來顯示用戶列表
                userListScrollPosition = GUILayout.BeginScrollView(userListScrollPosition, 
                    GUILayout.Width(130), 
                    GUILayout.Height(WINDOW_HEIGHT - 100));

                if (userList != null && userList.Count > 0)
                {
                    foreach (string user in userList.ToList())
                    {
                        bool isSelected = user == selectedUser;
                        bool isSelf = user == chatManager.UserName;
                        bool isHost = user == chatManager.hostName;  // 檢查是否為主機

                        // 設置顯示顏色
                        if (isSelf)
                        {
                            GUI.color = Color.gray;  // 自己顯示為灰色
                        }
                        else if (isHost)
                        {
                            GUI.color = Color.yellow;  // 主機顯示為黃色
                        }
                        else
                        {
                            GUI.color = isSelected ? Color.cyan : Color.white;  // 其他用戶
                        }

                        // 組合顯示文本
                        string displayName = user;
                        if (isSelf)
                        {
                            displayName += " (我)";
                        }
                        if (isHost)
                        {
                            displayName += " [房主]";
                        }

                        // 使用固定寬度的按鈕
                        if (GUILayout.Button(displayName, GUILayout.Width(110)))
                        {
                            // 只有點擊其他用戶時才更新選擇
                            if (!isSelf)
                            {
                                selectedUser = isSelected ? string.Empty : user;
                                Debug.Log($"選擇用戶: {selectedUser}");
                            }
                        }
                    }
                }
                else
                {
                    GUILayout.Label("沒有在線用戶");
                }

                GUI.color = Color.white;  // 重置顏色
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            catch (Exception ex)
            {
                Debug.LogError($"繪製用戶列表時發生錯誤: {ex}");
            }
        }

        private void SendMessage()
        {
            if (string.IsNullOrEmpty(inputMessage)) return;

            // 檢查是否試圖私聊自己
            if (!string.IsNullOrEmpty(selectedUser) && selectedUser == chatManager.UserName)
            {
                AddToLog("錯誤: 不能私聊自己");
                return;
            }

            if (string.IsNullOrEmpty(selectedUser))
            {
                chatManager.BroadcastChatMessage(inputMessage);
            }
            else
            {
                chatManager.SendPrivateMessage(selectedUser, inputMessage);
            }

            inputMessage = string.Empty;
        }

        private void HandleChatMessage(ChatMessage message)
        {
            string prefix = "";
            if (message.Type == MessageType.Private)
            {
                // 根據發送者和接收者來決定顯示格式
                if (message.FromName == chatManager.UserName)
                {
                    prefix = $"[私聊給 {message.ToName}]";
                }
                else
                {
                    prefix = $"[來自 {message.FromName} 的私聊]";
                }
            }

            string log = string.Format("[{0}] {1}{2}: {3}",
                System.DateTime.Now.ToString("HH:mm:ss"),
                prefix,
                message.FromName,
                message.Content);

            AddToLog(log);
        }

        private void HandleUserListUpdate(List<string> users)
        {
            // 保存用戶列表，等待房間準備好後再更新
            var pendingUsers = new List<string>(users);
            
            // 確保在主線程中更新 UI
            if (Thread.CurrentThread.IsBackground)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() => HandleUserListUpdateInternal(pendingUsers));
            }
            else
            {
                HandleUserListUpdateInternal(pendingUsers);
            }
        }

        private void HandleUserListUpdateInternal(List<string> users)
        {
            try
            {
                Debug.Log($"[用戶列表] 開始更新用戶列表 UI，用戶數: {users.Count}，用戶: {string.Join(", ", users)}");
                
                // 保存用戶列表
                userList = new List<string>(users);
                
                // 如果選中的用戶不在列表中，清除選擇
                if (!string.IsNullOrEmpty(selectedUser) && !users.Contains(selectedUser))
                {
                    selectedUser = string.Empty;
                }
                
                Debug.Log($"[用戶列表] 更新完成，當前列表: {string.Join(", ", userList)}");
                
                // 強制重繪 UI
                Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogError($"更新用戶列表 UI 失敗: {ex}");
            }
        }

        private void HandleRoomJoined(bool success)
        {
            // 確保在主線程中更新 UI
            if (Thread.CurrentThread.IsBackground)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() => HandleRoomJoinedInternal(success));
            }
            else
            {
                HandleRoomJoinedInternal(success);
            }
        }

        private void HandleRoomJoinedInternal(bool success)
        {
            try
            {
                Debug.Log($"[房間狀態] 處理房間狀態變更 - success: {success}, 當前是否在房間中: {isInRoom}");
                
                if (success)
                {
                    Debug.Log("開始處理房間加入成功事件");
                    
                    // 不要清空用戶列表，因為可能已經收到了
                    messageLog.Clear();
                    
                    // 重置輸入和選擇狀態
                    inputMessage = string.Empty;
                    selectedUser = string.Empty;
                    
                    // 重置滾動位置
                    scrollPosition = Vector2.zero;
                    
                    // 添加系統消息
                    AddToLog("成功" + (chatManager.IsHost ? "創建" : "加入") + "聊天室");
                    
                    // 設置房間狀態，這會觸發 UI 切換
                    Debug.Log("切換到聊天室界面");
                    isInRoom = true;

                    // 如果是主機，直接更新用戶列表
                    if (chatManager.IsHost)
                    {
                        var currentUsers = new List<string> { chatManager.UserName };
                        HandleUserListUpdateInternal(currentUsers);
                    }
                    else
                    {
                        Debug.Log($"當前用戶列表: {string.Join(", ", userList)}");
                    }
                }
                else
                {
                    Debug.Log("[房間狀態] 房間關閉或加入失敗，準備清理資源");
                    LeaveRoom();  // 使用 LeaveRoom 來處理清理工作
                    
                    // 強制更新 UI 狀態
                    isInRoom = false;
                    
                    // 添加提示消息
                    AddToLog(chatManager.IsHost ? "房間已關閉" : "主機已關閉房間");
                    
                    // 強制重繪 UI
                    Repaint();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"處理房間加入事件時發生錯誤: {ex}");
                LeaveRoom();
            }
        }

        private void HandleError(string error)
        {
            AddToLog("錯誤: " + error);
            // 如果是嚴重錯誤，可以考慮斷開連接
            // isInRoom = false;
        }

        void OnDestroy()
        {
            if (chatManager != null)
            {
                chatManager.LeaveChat();
                
                // 取消訂閱事件
                chatManager.OnChatMessageReceived -= HandleChatMessage;
                chatManager.OnUserListUpdated -= HandleUserListUpdate;
                chatManager.OnRoomJoined -= HandleRoomJoined;
                chatManager.OnError -= HandleError;
            }
        }

        // 添加一個方法來處理 Enter 鍵發送消息
        void Update()
        {
            if (isInRoom && Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrEmpty(inputMessage))
            {
                SendMessage();
            }
        }

        private void AddToLog(string message)
        {
            // 確保在主線程中更新 UI
            if (Thread.CurrentThread.IsBackground)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() => AddToLogInternal(message));
            }
            else
            {
                AddToLogInternal(message);
            }
        }

        private void AddToLogInternal(string message)
        {
            messageLog.Add(message);
            if (messageLog.Count > MAX_LOG_COUNT)
            {
                messageLog.RemoveAt(0);
            }
            // 強制滾動到底部
            scrollPosition.y = float.MaxValue;
        }

        private void LeaveRoom()
        {
            Debug.Log("[離開房間] 開始執行離開房間流程");
            
            if (chatManager != null)
            {
                chatManager.LeaveChat();
            }
            
            // 重置所有狀態
            isInRoom = false;
            messageLog.Clear();
            userList.Clear();
            inputMessage = string.Empty;
            selectedUser = string.Empty;
            scrollPosition = Vector2.zero;
            
            // 添加離開提示
            AddToLog("已離開聊天室");
            
            // 強制重繪 UI
            Repaint();
            
            Debug.Log("[離開房間] 離開房間流程完成");
        }

        private void Repaint()
        {
            // 強制在下一幀重繪 UI
            if (Thread.CurrentThread.IsBackground)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() => 
                {
                    Debug.Log("[UI] 強制重繪 UI");
                });
            }
        }
    }
} 