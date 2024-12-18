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

                // 左側聊天區域
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
                        GUI.color = isSelected ? Color.cyan : Color.white;

                        // 使用固定寬度的按鈕
                        if (GUILayout.Button(user, GUILayout.Width(110)))
                        {
                            selectedUser = isSelected ? string.Empty : user;
                            Debug.Log($"選擇用戶: {selectedUser}");
                        }
                    }
                }
                else
                {
                    GUILayout.Label("沒有在線用戶");
                }

                GUI.color = Color.white;
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
            string prefix = message.Type == MessageType.Private ? "[私聊]" : "";
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
                Debug.Log($"開始更新用戶列表 UI，用戶數: {users.Count}，用戶: {string.Join(", ", users)}");
                
                // 保存用戶列表，即使還沒進入房間
                userList = new List<string>(users);
                
                Debug.Log($"用戶列表更新完成，當前列表: {string.Join(", ", userList)}");
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
                    Debug.Log("房間加入失敗");
                    isInRoom = false;
                    AddToLog("加入聊天室失敗");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"處理房間加入事件時發生錯誤: {ex}");
                isInRoom = false;
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
        }

        private void Repaint()
        {
            // 強制在下一幀��繪 UI
            if (Thread.CurrentThread.IsBackground)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() => { });
            }
        }
    }
} 