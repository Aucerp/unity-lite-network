using UnityEngine;
using System;

namespace Unet.EventDrivenChat
{
    public class PortraitChatRoomUI : BaseChatRoomUI
    {
        private PortraitLayout layout;

        void Awake()
        {
            layout = new PortraitLayout();
        }

        protected override void DrawLoginWindow()
        {
            // 使用深色半透明背景
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            GUI.color = Color.white;

            // 繪製標題欄
            GUI.Box(new Rect(0, 0, Screen.width, PortraitLayout.TITLE_BAR_HEIGHT), "", layout.GetTitleBarStyle());
            GUI.Label(new Rect(0, 0, Screen.width, PortraitLayout.TITLE_BAR_HEIGHT), "聊天室登入", layout.GetLoginTitleStyle());

            // 繪製主窗口
            GUI.Box(new Rect(0, PortraitLayout.TITLE_BAR_HEIGHT, Screen.width, Screen.height - PortraitLayout.TITLE_BAR_HEIGHT), "", layout.GetWindowBodyStyle());

            // 登入窗口內容
            GUILayout.BeginArea(new Rect(PortraitLayout.MARGIN, 
                PortraitLayout.TITLE_BAR_HEIGHT + PortraitLayout.MARGIN, 
                Screen.width - PortraitLayout.MARGIN * 2, 
                Screen.height - PortraitLayout.TITLE_BAR_HEIGHT - PortraitLayout.MARGIN * 2));
            
            DrawLoginWindowContent(0);
            
            GUILayout.EndArea();
        }

        protected override void DrawLoginWindowContent(int windowID)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

                // 用戶名稱輸入
                GUILayout.Label("用戶名稱:", layout.GetLoginLabelStyle(), GUILayout.Width(layout.LoginLabelWidth));
                userName = GUILayout.TextField(userName, layout.GetLoginInputStyle(), GUILayout.Height(layout.LoginInputHeight));

                GUILayout.Space(20);

                // 主機設置區域
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("建立房間:", layout.GetLoginLabelStyle());
                
                // 主機端口設置
                GUILayout.BeginHorizontal();
                GUILayout.Label("主機端口:", layout.GetLoginLabelStyle(), GUILayout.Width(layout.LoginLabelWidth));
                string hostPortStr = GUILayout.TextField(hostPort.ToString(), layout.GetLoginInputStyle(), 
                    GUILayout.Width(layout.LoginLabelWidth), GUILayout.Height(layout.LoginInputHeight));
                int.TryParse(hostPortStr, out hostPort);
                GUILayout.EndHorizontal();

                GUILayout.Space(20);
                
                // 建立房間按鈕
                if (GUILayout.Button("建立房間", layout.GetLoginButtonStyle(), GUILayout.Height(layout.LoginButtonHeight)))
                {
                    chatRoom.CreateRoom(userName, hostPort);
                }
                GUILayout.EndVertical();

                GUILayout.Space(20);

                // 客戶端設置區域
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("加入現有房間:", layout.GetLoginLabelStyle());

                // IP 輸入
                GUILayout.BeginHorizontal();
                GUILayout.Label("主機 IP:", layout.GetLoginLabelStyle(), GUILayout.Width(layout.LoginLabelWidth));
                hostIP = GUILayout.TextField(hostIP, layout.GetLoginInputStyle(), 
                    GUILayout.ExpandWidth(true), GUILayout.Height(layout.LoginInputHeight));
                GUILayout.EndHorizontal();

                // 端口輸入
                GUILayout.BeginHorizontal();
                GUILayout.Label("連接埠:", layout.GetLoginLabelStyle(), GUILayout.Width(layout.LoginLabelWidth));
                string portStr = GUILayout.TextField(hostPort.ToString(), layout.GetLoginInputStyle(), 
                    GUILayout.Width(layout.LoginLabelWidth), GUILayout.Height(layout.LoginInputHeight));
                int.TryParse(portStr, out hostPort);
                GUILayout.EndHorizontal();

                GUILayout.Space(20);
                
                // 加入房間按鈕
                if (GUILayout.Button("加入房間", layout.GetLoginButtonStyle(), GUILayout.Height(layout.LoginButtonHeight)))
                {
                    int randomPort = UnityEngine.Random.Range(11001, 12000);
                    chatRoom.JoinRoom(userName, randomPort, hostIP, hostPort);
                }
                GUILayout.EndVertical();

                // 顯示錯誤消息
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    GUILayout.Space(10);
                    var style = new GUIStyle(layout.GetLoginLabelStyle());
                    style.normal.textColor = Color.red;
                    style.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label(errorMessage, style);
                }

                GUILayout.EndVertical();
                GUILayout.Space(10);
            }
            GUILayout.EndVertical();
        }

        protected override void DrawChatWindow()
        {
            // 使用深色半透明背景
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            GUI.color = Color.white;

            // 繪製標題欄
            GUI.Box(new Rect(0, 0, Screen.width, PortraitLayout.TITLE_BAR_HEIGHT), "", layout.GetTitleBarStyle());
            GUI.Label(new Rect(0, 0, Screen.width, PortraitLayout.TITLE_BAR_HEIGHT), "聊天室", layout.GetLoginTitleStyle());

            // 繪製關閉按鈕
            GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
            if (GUI.Button(layout.CloseButtonRect, "✕", layout.GetLoginButtonStyle()))
            {
                chatRoom.LeaveRoom();
            }
            GUI.backgroundColor = Color.white;

            // 繪製主窗口
            GUI.Box(new Rect(0, PortraitLayout.TITLE_BAR_HEIGHT, Screen.width, Screen.height - PortraitLayout.TITLE_BAR_HEIGHT), "", layout.GetWindowBodyStyle());

            // 聊天窗口內容 - 從標題欄底部開始
            GUILayout.BeginArea(new Rect(0, 
                PortraitLayout.TITLE_BAR_HEIGHT, 
                Screen.width, 
                Screen.height - PortraitLayout.TITLE_BAR_HEIGHT));

            DrawWindow(0);

            GUILayout.EndArea();
        }

        protected override void DrawWindow(int windowID)
        {
            try
            {
                // 1. 水平用戶列表 - 緊貼標題欄底部
                GUILayout.BeginArea(new Rect(0, 0, layout.WindowWidth, layout.UserListHeight));
                userListScrollPosition = GUILayout.BeginScrollView(
                    userListScrollPosition, 
                    false,  // 禁用垂直滾動
                    true,   // 啟用水平滾動
                    GUILayout.Height(layout.UserListHeight),
                    GUILayout.Width(layout.WindowWidth)
                );

                DrawUserList();

                GUILayout.EndScrollView();
                
                // 私聊狀態
                if (!string.IsNullOrEmpty(selectedUser))
                {
                    GUI.color = PRIVATE_CHAT_COLOR;
                    GUILayout.Label($"→{selectedUser}", layout.GetCurrentChatStyle(), GUILayout.Width(80));
                }
                GUILayout.EndArea();

                // 2. 聊天區域 - 從用戶列表底部開始
                float chatY = layout.UserListHeight;
                GUILayout.BeginArea(new Rect(0, chatY, layout.WindowWidth, layout.ChatAreaHeight));
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                foreach (string message in messageLog)
                {
                    GUILayout.Label(message, layout.GetCurrentChatStyle());
                }
                GUILayout.EndScrollView();
                GUILayout.EndArea();

                // 3. 輸入區域
                float inputY = chatY + layout.ChatAreaHeight;
                GUILayout.BeginArea(new Rect(0, inputY, layout.WindowWidth, layout.InputAreaHeight));
                GUILayout.BeginHorizontal();
                
                // 輸入框
                GUI.SetNextControlName("ChatInput");
                inputMessage = GUILayout.TextField(
                    inputMessage, 
                    layout.GetCurrentInputStyle(), 
                    GUILayout.Width(layout.InputFieldWidth)
                );

                // 發送按鈕
                if (GUILayout.Button("發送", 
                    layout.GetSendButtonStyle(),
                    GUILayout.Width(layout.SendButtonWidth)))
                {
                    if (!string.IsNullOrEmpty(inputMessage))
                    {
                        SendChatMessageToRoom();
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.EndArea();

                // 保持輸入框焦點
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.FocusControl("ChatInput");
                }

                GUI.color = Color.white;
            }
            catch (Exception ex)
            {
                Debug.LogError($"繪製聊天窗口時發生錯誤: {ex}");
            }
        }

        private void DrawUserList()
        {
            Color originalColor = GUI.color;  // 保存原始顏色
            
            GUILayout.BeginHorizontal();
            foreach (string user in userList)
            {
                bool isSelected = user == selectedUser;
                bool isSelf = user == chatRoom.UserName;
                bool isHost = user == chatRoom.HostName;

                // 設置用戶按鈕顏色
                if (isHost) GUI.color = HOST_COLOR;
                else if (isSelected) GUI.color = PRIVATE_CHAT_COLOR;
                else GUI.color = AssignUserColor(user);

                string displayName = user;
                if (isSelf) displayName = "*" + displayName;
                if (isHost) displayName += "★";

                if (GUILayout.Button(displayName, layout.GetCurrentButtonStyle(), 
                    GUILayout.Width(layout.UserButtonWidth),
                    GUILayout.Height(layout.UserListHeight)))
                {
                    selectedUser = (isSelf || isSelected) ? string.Empty : user;
                }
            }
            GUILayout.EndHorizontal();

            GUI.color = originalColor;  // 恢復原始顏色
        }

        private void DrawChatArea()
        {
            Color originalColor = GUI.color;
            GUI.color = Color.white;
            
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
            
            GUI.color = originalColor;
        }

        private void DrawInputArea()
        {
            Color originalInputColor = GUI.color;
            GUI.color = Color.white;

            Rect inputAreaRect = new Rect(
                0,
                layout.WindowHeight - layout.InputAreaHeight,
                layout.WindowWidth,
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
                    GUILayout.Height(layout.InputAreaHeight)
                );

                // 發送按鈕
                if (GUILayout.Button("發送", 
                    layout.GetSendButtonStyle(),
                    GUILayout.Width(layout.SendButtonWidth),
                    GUILayout.Height(layout.InputAreaHeight)))
                {
                    if (!string.IsNullOrEmpty(inputMessage))
                    {
                        SendChatMessageToRoom();
                    }
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();

            GUI.color = originalInputColor;
        }

        protected override string FormatMessage(ChatMessage message, string colorHex, string privateColorHex)
        {
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
            return $"{prefix}<color=#{colorHex}>{message.FromName}</color>: {message.Content}";
        }
    }
} 
