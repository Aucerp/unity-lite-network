using UnityEngine;

namespace Unet.EventDrivenChat
{
    public class PortraitLayout
    {
        // 常量定義
        public const float MARGIN = 20f;
        public const float INNER_MARGIN = 10f;
        public const float TITLE_BAR_HEIGHT = 44f;

        // 聊天界面字體大小
        private const int BUTTON_FONT_SIZE = 18;
        private const int CHAT_FONT_SIZE = 20;
        private const int INPUT_FONT_SIZE = 20;
        private const int SEND_BUTTON_FONT_SIZE = 20;

        // 登入界面字體大小
        private const int LOGIN_LABEL_FONT_SIZE = 24;
        private const int LOGIN_INPUT_FONT_SIZE = 24;
        private const int LOGIN_BUTTON_FONT_SIZE = 24;
        private const int LOGIN_TITLE_FONT_SIZE = 28;

        // 窗口尺寸
        public float WindowWidth { get; private set; }
        public float WindowHeight { get; private set; }
        public Rect LoginWindowRect { get; private set; }
        public Rect ChatWindowRect { get; private set; }
        public Rect CloseButtonRect { get; private set; }

        // 用戶列表區域
        public float UserListHeight { get; private set; }
        public float UserButtonWidth { get; private set; }

        // 聊天區域
        public float ChatAreaWidth { get; private set; }
        public float ChatAreaHeight { get; private set; }

        // 輸入區域
        public float InputAreaHeight { get; private set; }
        public float InputFieldWidth { get; private set; }
        public float SendButtonWidth { get; private set; }

        // 登入界面尺寸
        public float LoginInputHeight { get; private set; }
        public float LoginButtonHeight { get; private set; }
        public float LoginLabelWidth { get; private set; }

        // 樣式緩存
        private GUIStyle buttonStyle;
        private GUIStyle chatStyle;
        private GUIStyle inputStyle;
        private GUIStyle sendButtonStyle;
        private GUIStyle loginLabelStyle;
        private GUIStyle loginInputStyle;
        private GUIStyle loginButtonStyle;
        private GUIStyle loginTitleStyle;

        // 新增屬性
        public float InputAreaY { get; private set; }

        public PortraitLayout()
        {
            UpdateLayout();
        }

        public void UpdateLayout()
        {
            // 基礎尺寸計算 - 使用全螢幕
            WindowWidth = Screen.width;
            WindowHeight = Screen.height;
            
            // 登入窗口 - 使用固定寬度和高度，並置於上方中央
            float loginWidth = Mathf.Min(600, WindowWidth * 0.9f);
            float loginHeight = WindowHeight * 0.7f;
            float topMargin = WindowHeight * 0.05f;
            LoginWindowRect = new Rect(
                (WindowWidth - loginWidth) / 2,
                topMargin,
                loginWidth,
                loginHeight
            );

            // 登入界面元素尺寸
            LoginInputHeight = 60;
            LoginButtonHeight = 80;
            LoginLabelWidth = 140;

            // 聊天窗口 - 使用全螢幕
            ChatWindowRect = new Rect(0, 0, WindowWidth, WindowHeight);

            // 關閉按鈕 - 調整到標題欄右上角
            CloseButtonRect = new Rect(
                WindowWidth - 41,
                5,
                36,
                36
            );

            // 用戶列表區域
            UserListHeight = 45;
            UserButtonWidth = 80;

            // 輸入區域
            InputAreaHeight = 40;
            SendButtonWidth = 60;
            InputFieldWidth = WindowWidth - SendButtonWidth;

            // 聊天區域 - 精確計算高度，並為用戶列表下方添加間距
            ChatAreaWidth = WindowWidth;
            float totalUsedHeight = TITLE_BAR_HEIGHT + UserListHeight + InputAreaHeight + 10;
            ChatAreaHeight = WindowHeight - totalUsedHeight;

            // 更新輸入區域的位置
            InputAreaY = WindowHeight - InputAreaHeight - (WindowHeight - Screen.height);
        }

        public Rect GetChatAreaRect()
        {
            return new Rect(
                0,
                TITLE_BAR_HEIGHT + UserListHeight,
                ChatAreaWidth,
                ChatAreaHeight
            );
        }

        public GUIStyle GetCurrentButtonStyle()
        {
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = BUTTON_FONT_SIZE;
                buttonStyle.alignment = TextAnchor.MiddleCenter;
                buttonStyle.padding = new RectOffset(8, 8, 6, 6);  // 增加按鈕文字的內邊距
                buttonStyle.margin = new RectOffset(2, 2, 2, 2);   // 增加按鈕之間的間距
            }
            return buttonStyle;
        }

        public GUIStyle GetCurrentChatStyle()
        {
            if (chatStyle == null)
            {
                chatStyle = new GUIStyle(GUI.skin.label);
                chatStyle.fontSize = CHAT_FONT_SIZE;
                chatStyle.wordWrap = true;
                chatStyle.richText = true;
                chatStyle.padding = new RectOffset(10, 10, 4, 4);  // 增加聊天文字的左右內邊距和上下間距
                chatStyle.margin = new RectOffset(0, 0, 2, 2);     // 增加聊天行之間的間距
            }
            return chatStyle;
        }

        public GUIStyle GetCurrentInputStyle()
        {
            if (inputStyle == null)
            {
                inputStyle = new GUIStyle(GUI.skin.textField);
                inputStyle.fontSize = INPUT_FONT_SIZE;
                inputStyle.alignment = TextAnchor.MiddleLeft;
                inputStyle.padding = new RectOffset(8, 8, 8, 8);  // 調整內邊距
                inputStyle.margin = new RectOffset(0, 0, 0, 0);
                inputStyle.fixedHeight = InputAreaHeight;  // 固定高度
            }
            return inputStyle;
        }

        public GUIStyle GetSendButtonStyle()
        {
            if (sendButtonStyle == null)
            {
                sendButtonStyle = new GUIStyle(GUI.skin.button);
                sendButtonStyle.fontSize = SEND_BUTTON_FONT_SIZE;
                sendButtonStyle.alignment = TextAnchor.MiddleCenter;
                sendButtonStyle.padding = new RectOffset(4, 4, 4, 4);
                sendButtonStyle.margin = new RectOffset(0, 0, 0, 0);
                sendButtonStyle.fixedHeight = InputAreaHeight;  // 固定高度
            }
            return sendButtonStyle;
        }

        public GUIStyle GetLoginLabelStyle()
        {
            if (loginLabelStyle == null)
            {
                loginLabelStyle = new GUIStyle(GUI.skin.label);
                loginLabelStyle.fontSize = LOGIN_LABEL_FONT_SIZE;
                loginLabelStyle.alignment = TextAnchor.MiddleLeft;
                loginLabelStyle.padding = new RectOffset(8, 8, 8, 8);
            }
            return loginLabelStyle;
        }

        public GUIStyle GetLoginInputStyle()
        {
            if (loginInputStyle == null)
            {
                loginInputStyle = new GUIStyle(GUI.skin.textField);
                loginInputStyle.fontSize = LOGIN_INPUT_FONT_SIZE;
                loginInputStyle.alignment = TextAnchor.MiddleLeft;
                loginInputStyle.padding = new RectOffset(12, 12, 8, 8);
            }
            return loginInputStyle;
        }

        public GUIStyle GetLoginButtonStyle()
        {
            if (loginButtonStyle == null)
            {
                loginButtonStyle = new GUIStyle(GUI.skin.button);
                loginButtonStyle.fontSize = LOGIN_BUTTON_FONT_SIZE;
                loginButtonStyle.alignment = TextAnchor.MiddleCenter;
                loginButtonStyle.padding = new RectOffset(15, 15, 12, 12);
                loginButtonStyle.margin = new RectOffset(0, 0, 8, 8);
            }
            return loginButtonStyle;
        }

        public GUIStyle GetLoginTitleStyle()
        {
            if (loginTitleStyle == null)
            {
                loginTitleStyle = new GUIStyle(GUI.skin.label);
                loginTitleStyle.fontSize = LOGIN_TITLE_FONT_SIZE;
                loginTitleStyle.alignment = TextAnchor.MiddleCenter;
                loginTitleStyle.normal.textColor = Color.white;
                loginTitleStyle.fontStyle = FontStyle.Bold;
                loginTitleStyle.padding = new RectOffset(0, 0, 0, 0);
            }
            return loginTitleStyle;
        }

        public GUIStyle GetTitleBarStyle()
        {
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = MakeTexture(1, 1, new Color(0.2f, 0.2f, 0.2f, 1f));
            style.padding = new RectOffset(10, 10, 5, 5);
            return style;
        }

        public GUIStyle GetWindowBodyStyle()
        {
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = MakeTexture(1, 1, new Color(0.15f, 0.15f, 0.15f, 0.95f));
            style.padding = new RectOffset(0, 0, 0, 0);
            style.margin = new RectOffset(0, 0, 0, 0);
            return style;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
} 
