using UnityEngine;

namespace Unet.EventDrivenChat
{
    public class LandscapeLayout
    {
        // 常量定義
        public const float MARGIN = 0f;
        public const float INNER_MARGIN = 5f;
        
        // 窗口尺寸
        public float WindowWidth { get; private set; }
        public float WindowHeight { get; private set; }
        public Rect WindowRect { get; private set; }
        public Rect LoginWindowRect { get; private set; }
        public Rect ChatWindowRect { get; private set; }
        public Rect CloseButtonRect { get; private set; }

        // 用戶列表區域
        public float UserListWidth { get; private set; }

        // 聊天區域
        public float ChatAreaWidth { get; private set; }
        public float ChatAreaHeight { get; private set; }

        // 輸入區域
        public float InputAreaHeight { get; private set; }
        public float InputFieldWidth { get; private set; }
        public float SendButtonWidth { get; private set; }

        // 樣式緩存
        private GUIStyle buttonStyle;
        private GUIStyle chatStyle;
        private GUIStyle inputStyle;
        private GUIStyle sendButtonStyle;

        public LandscapeLayout()
        {
            UpdateLayout();
        }

        public void UpdateLayout()
        {
            // 基礎尺寸計算
            WindowWidth = Screen.width * 0.95f;
            WindowHeight = Screen.height * 0.95f;
            
            // 登入窗口
            float loginWidth = Mathf.Min(500, WindowWidth * 0.4f);
            float loginHeight = WindowHeight * 0.7f;
            LoginWindowRect = new Rect(
                Screen.width/2 - loginWidth/2,
                Screen.height/2 - loginHeight/2,
                loginWidth,
                loginHeight
            );

            // 聊天窗口
            ChatWindowRect = new Rect(
                Screen.width/2 - WindowWidth/2,
                Screen.height/2 - WindowHeight/2,
                WindowWidth,
                WindowHeight
            );

            // 關閉按鈕
            CloseButtonRect = new Rect(
                WindowWidth - 30,
                5,
                25,
                25
            );

            // 用戶列表區域
            UserListWidth = WindowWidth * 0.2f;

            // 輸入區域
            InputAreaHeight = 40;
            SendButtonWidth = 80;
            InputFieldWidth = WindowWidth - UserListWidth - SendButtonWidth - (MARGIN * 6);

            // 聊天區域
            ChatAreaWidth = WindowWidth - UserListWidth - (MARGIN * 3);
            ChatAreaHeight = WindowHeight - InputAreaHeight - (MARGIN * 3);
        }

        public GUIStyle GetCurrentButtonStyle()
        {
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = 14;
                buttonStyle.alignment = TextAnchor.MiddleLeft;
                buttonStyle.padding = new RectOffset(10, 10, 5, 5);
            }
            return buttonStyle;
        }

        public GUIStyle GetCurrentChatStyle()
        {
            if (chatStyle == null)
            {
                chatStyle = new GUIStyle(GUI.skin.label);
                chatStyle.fontSize = 16;
                chatStyle.wordWrap = true;
                chatStyle.richText = true;
            }
            return chatStyle;
        }

        public GUIStyle GetCurrentInputStyle()
        {
            if (inputStyle == null)
            {
                inputStyle = new GUIStyle(GUI.skin.textField);
                inputStyle.fontSize = 16;
                inputStyle.alignment = TextAnchor.MiddleLeft;
                inputStyle.padding = new RectOffset(10, 10, 5, 5);
            }
            return inputStyle;
        }

        public GUIStyle GetSendButtonStyle()
        {
            if (sendButtonStyle == null)
            {
                sendButtonStyle = new GUIStyle(GUI.skin.button);
                sendButtonStyle.fontSize = 16;
                sendButtonStyle.alignment = TextAnchor.MiddleCenter;
                sendButtonStyle.padding = new RectOffset(5, 5, 5, 5);
            }
            return sendButtonStyle;
        }
    }
} 
