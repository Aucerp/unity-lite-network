using UnityEngine;
using System;

namespace Unet.EventDrivenChat
{
    public class LandscapeChatRoomUI : BaseChatRoomUI
    {
        private LandscapeLayout layout;

        void Awake()
        {
            layout = new LandscapeLayout();
        }

        protected override void DrawLoginWindow()
        {
            GUILayout.Window(1, layout.LoginWindowRect, DrawLoginWindowContent, "聊天室登入");
        }

        protected override void DrawLoginWindowContent(int windowID)
        {
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
            
            // 建立房間按鈕
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
            
            // 加入房間按鈕
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

        protected override void DrawChatWindow()
        {
            GUILayout.Window(0, layout.ChatWindowRect, DrawWindow, GetWindowTitle());
        }

        protected override void DrawWindow(int windowID)
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

                GUILayout.BeginHorizontal();

                // 左側用戶列表
                DrawUserListArea();

                // 右側聊天區域
                DrawChatArea();

                GUILayout.EndHorizontal();

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

        private void DrawUserListArea()
        {
            GUILayout.BeginVertical(GUILayout.Width(layout.UserListWidth));
            
            userListScrollPosition = GUILayout.BeginScrollView(userListScrollPosition);

            foreach (string user in userList)
            {
                bool isSelected = user == selectedUser;
                bool isSelf = user == chatRoom.UserName;
                bool isHost = user == chatRoom.HostName;

                if (isHost) GUI.color = HOST_COLOR;
                else if (isSelected) GUI.color = PRIVATE_CHAT_COLOR;
                else GUI.color = AssignUserColor(user);

                string displayName = user;
                if (isSelf) displayName = "*" + displayName;
                if (isHost) displayName += "★";

                if (GUILayout.Button(displayName, layout.GetCurrentButtonStyle()))
                {
                    selectedUser = (isSelf || isSelected) ? string.Empty : user;
                }
            }

            GUILayout.EndScrollView();

            // 私聊狀態
            if (!string.IsNullOrEmpty(selectedUser))
            {
                GUI.color = PRIVATE_CHAT_COLOR;
                GUILayout.Label($"私聊對象: {selectedUser}", layout.GetCurrentChatStyle());
            }

            GUILayout.EndVertical();
        }

        private void DrawChatArea()
        {
            GUILayout.BeginVertical();

            // 聊天記錄區域
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

            // 輸入區域
            GUILayout.BeginHorizontal(GUILayout.Height(layout.InputAreaHeight));

            GUI.SetNextControlName("ChatInput");
            inputMessage = GUILayout.TextField(
                inputMessage, 
                layout.GetCurrentInputStyle(), 
                GUILayout.Width(layout.InputFieldWidth)
            );

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

            GUILayout.EndVertical();
        }

        protected override string FormatMessage(ChatMessage message, string colorHex, string privateColorHex)
        {
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
            return $"[{DateTime.Now:HH:mm:ss}] {prefix}<color=#{colorHex}>{message.FromName}</color>: {message.Content}";
        }
    }
} 
