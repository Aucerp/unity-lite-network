using UnityEngine;

namespace Unet.EventDrivenChat
{
    public class ChatRoomUILayout
    {
        #region Properties
        // 窗口基本設置
        public float WindowWidth { get; private set; }
        public float WindowHeight { get; private set; }
        public bool IsPortrait { get; private set; }

        // 布局常量
        private const float LANDSCAPE_RATIO = 16f / 9f;   // 橫向比例 1280x720
        private const float PORTRAIT_RATIO = 9f / 16f;    // 縱向比例 540x960
        public const float MARGIN = 10f;                  // 基本邊距
        public const float INNER_MARGIN = 5f;             // 內部元素邊距
        private const float SCROLL_BAR_WIDTH = 20f;       // 滾動條寬度
        private const float CONTENT_PADDING = 15f;        // 內容區域內邊距

        // 登錄窗口
        public Rect LoginWindowRect { get; private set; }

        // 主聊天窗口
        public Rect ChatWindowRect { get; private set; }
        public Rect CloseButtonRect { get; private set; }
        
        // 內容區域
        public float ChatAreaWidth { get; private set; }
        public float ChatAreaHeight { get; private set; }
        public float UserListWidth { get; private set; }
        public float UserListHeight { get; private set; }
        public float UserButtonWidth { get; private set; }
        
        // 輸入區域
        public float InputAreaHeight { get; private set; }
        public float InputFieldWidth { get; private set; }
        public float SendButtonWidth { get; private set; }

        // 添加字體大小設定
        private const float PORTRAIT_FONT_SIZE = 16f;     // 縱向模式字體大小改小
        private const float PORTRAIT_TITLE_SIZE = 20f;    // 縱向模式標題字體大小
        private const float LANDSCAPE_FONT_SIZE = 12f;    // 橫向模式字體大小保持不變

        public float CurrentFontSize { get; private set; }

        // 添加聊天室樣式
        private GUIStyle _chatStyle;
        public GUIStyle ChatStyle 
        { 
            get 
            {
                if (_chatStyle == null)
                {
                    _chatStyle = new GUIStyle(GUI.skin.label);
                    _chatStyle.fontSize = (int)(IsPortrait ? PORTRAIT_FONT_SIZE : LANDSCAPE_FONT_SIZE);
                    _chatStyle.wordWrap = true;
                    _chatStyle.richText = true;
                }
                return _chatStyle;
            }
        }

        private GUIStyle _buttonStyle;
        public GUIStyle ButtonStyle
        {
            get
            {
                if (_buttonStyle == null)
                {
                    _buttonStyle = new GUIStyle(GUI.skin.button);
                    _buttonStyle.fontSize = (int)(IsPortrait ? PORTRAIT_FONT_SIZE : LANDSCAPE_FONT_SIZE);
                }
                return _buttonStyle;
            }
        }

        // 添加不同的樣式
        private GUIStyle _portraitChatStyle;
        public GUIStyle PortraitChatStyle 
        { 
            get 
            {
                if (_portraitChatStyle == null)
                {
                    _portraitChatStyle = new GUIStyle(GUI.skin.label);
                    _portraitChatStyle.fontSize = (int)PORTRAIT_FONT_SIZE;
                    _portraitChatStyle.wordWrap = true;
                    _portraitChatStyle.richText = true;
                    _portraitChatStyle.padding = new RectOffset(5, 5, 2, 2);
                }
                return _portraitChatStyle;
            }
        }

        public GUIStyle GetCurrentChatStyle()
        {
            if (IsPortrait)
            {
                var style = new GUIStyle(GUI.skin.label);
                style.fontSize = (int)PORTRAIT_FONT_SIZE;
                style.wordWrap = true;
                style.richText = true;
                style.padding = new RectOffset(5, 5, 2, 2);
                return style;
            }
            return ChatStyle;
        }

        public GUIStyle GetCurrentButtonStyle()
        {
            if (IsPortrait)
            {
                var style = new GUIStyle(GUI.skin.button);
                style.fontSize = (int)(PORTRAIT_FONT_SIZE * 0.8f);
                style.padding = new RectOffset(4, 4, 2, 2);
                style.alignment = TextAnchor.MiddleLeft;
                style.margin = new RectOffset(0, 0, 0, 0);
                style.clipping = TextClipping.Clip;
                return style;
            }
            return ButtonStyle;
        }

        public GUIStyle GetCurrentInputStyle()
        {
            var style = new GUIStyle(GUI.skin.textField);
            style.fontSize = (int)(PORTRAIT_FONT_SIZE);
            style.padding = new RectOffset(8, 8, 4, 4);
            style.alignment = TextAnchor.MiddleLeft;
            return style;
        }

        public GUIStyle GetSendButtonStyle()
        {
            var style = new GUIStyle(GUI.skin.button);
            style.fontSize = (int)(PORTRAIT_FONT_SIZE * 0.9f);
            style.padding = new RectOffset(4, 4, 2, 2);
            style.alignment = TextAnchor.MiddleCenter;
            return style;
        }

        public GUIStyle GetTitleStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = (int)(IsPortrait ? PORTRAIT_TITLE_SIZE : LANDSCAPE_FONT_SIZE);
            style.fontStyle = FontStyle.Bold;
            return style;
        }
        #endregion

        public ChatRoomUILayout()
        {
            CalculateLayout();
        }

        private void CalculateLayout()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            // 判斷螢幕方向
            IsPortrait = screenHeight > screenWidth;
            
            if (IsPortrait)
            {
                CalculatePortraitLayout(screenWidth, screenHeight);
            }
            else
            {
                CalculateLandscapeLayout(screenWidth, screenHeight);
            }

            // 算共用元素
            CalculateCommonElements(screenWidth, screenHeight);
        }

        #region Layout Calculations
        private void CalculatePortraitLayout(float screenWidth, float screenHeight)
        {
            // 縱向模式使用全螢幕
            WindowWidth = screenWidth;
            WindowHeight = screenHeight;

            float availableWidth = WindowWidth - (MARGIN * 2);

            // 用戶列表區域（頂部）
            UserListHeight = 35f;  // 減小高度
            UserListWidth = availableWidth;
            UserButtonWidth = 60f;  // 設置基礎按鈕寬度

            // 輸入區域（底部）
            InputAreaHeight = 40f;
            SendButtonWidth = 70f;
            InputFieldWidth = availableWidth - SendButtonWidth - MARGIN;

            // 聊天區域（中間）
            ChatAreaWidth = availableWidth;
            ChatAreaHeight = WindowHeight - UserListHeight - InputAreaHeight - (MARGIN * 4);

            CurrentFontSize = PORTRAIT_FONT_SIZE;
        }

        private void CalculateLandscapeLayout(float screenWidth, float screenHeight)
        {
            // 以 1280x720 為基準
            WindowWidth = 1280f;
            WindowHeight = WindowWidth / LANDSCAPE_RATIO;

            // 自動縮放
            if (WindowWidth > screenWidth * 0.9f)
            {
                WindowWidth = screenWidth * 0.9f;
                WindowHeight = WindowWidth / LANDSCAPE_RATIO;
            }
            if (WindowHeight > screenHeight * 0.9f)
            {
                WindowHeight = screenHeight * 0.9f;
                WindowWidth = WindowHeight * LANDSCAPE_RATIO;
            }

            // 用戶列表區域（右側）
            UserListWidth = WindowWidth * 0.2f;
            UserListHeight = WindowHeight - (MARGIN * 8);
            UserButtonWidth = UserListWidth - (MARGIN * 2);

            // 聊天區域（左側）
            ChatAreaWidth = WindowWidth - UserListWidth - (MARGIN * 3);
            ChatAreaHeight = WindowHeight - (MARGIN * 8);

            // 輸入區域（底部）
            InputAreaHeight = 30f;
            SendButtonWidth = 80f;
            InputFieldWidth = ChatAreaWidth - SendButtonWidth - (MARGIN * 2);

            // 設置當前字體大小
            CurrentFontSize = LANDSCAPE_FONT_SIZE;
        }

        private void CalculateCommonElements(float screenWidth, float screenHeight)
        {
            // 登入窗口
            float loginWidth = IsPortrait ? screenWidth * 0.95f : 400;
            float loginHeight = IsPortrait ? screenHeight * 0.6f : 300;
            
            LoginWindowRect = new Rect(
                screenWidth / 2 - loginWidth / 2,
                screenHeight / 2 - loginHeight / 2,
                loginWidth,
                loginHeight
            );

            // 縱向模式下聊天窗口佔滿螢幕
            if (IsPortrait)
            {
                ChatWindowRect = new Rect(0, 0, WindowWidth, WindowHeight);
            }
            else
            {
                // 橫向模式保持原有的居中顯示
                ChatWindowRect = new Rect(
                    Mathf.Max(MARGIN, screenWidth / 2 - WindowWidth / 2),
                    Mathf.Max(MARGIN, screenHeight / 2 - WindowHeight / 2),
                    WindowWidth,
                    WindowHeight
                );
            }

            // 關閉按鈕位置調整
            CloseButtonRect = new Rect(
                WindowWidth - 40,  // 稍微遠離邊緣
                5,
                35,               // 稍微加按鈕
                25
            );
        }
        #endregion

        #region Layout Getters
        public Rect GetStatusLabelRect()
        {
            if (IsPortrait)
            {
                return new Rect(MARGIN, 5, WindowWidth - (MARGIN * 2), 25);
            }
            else
            {
                return new Rect(MARGIN, 5, WindowWidth - UserListWidth - (MARGIN * 2), 25);
            }
        }

        public float GetUserListHeaderHeight()
        {
            return 25f;
        }

        public float GetContentAreaHeight()
        {
            if (IsPortrait)
            {
                return WindowHeight - InputAreaHeight - (MARGIN * 3);
            }
            else
            {
                return WindowHeight - InputAreaHeight - (MARGIN * 4);
            }
        }

        public Rect GetUserListRect()
        {
            if (IsPortrait)
            {
                float effectiveWidth = UserListWidth - SCROLL_BAR_WIDTH;
                return new Rect(
                    MARGIN,
                    MARGIN * 2,
                    effectiveWidth,
                    UserListHeight
                );
            }
            else
            {
                return new Rect(
                    WindowWidth - UserListWidth - MARGIN, 
                    MARGIN * 2, 
                    UserListWidth, 
                    UserListHeight
                );
            }
        }

        public Rect GetChatAreaRect()
        {
            if (IsPortrait)
            {
                return new Rect(
                    MARGIN,
                    UserListHeight + MARGIN,  // 減少頂部間距
                    ChatAreaWidth - SCROLL_BAR_WIDTH,
                    ChatAreaHeight
                );
            }
            else
            {
                return new Rect(
                    MARGIN, 
                    MARGIN * 3, 
                    ChatAreaWidth, 
                    ChatAreaHeight
                );
            }
        }

        public Rect GetInputAreaRect()
        {
            if (IsPortrait)
            {
                return new Rect(
                    MARGIN,
                    WindowHeight - InputAreaHeight - MARGIN,
                    WindowWidth - (MARGIN * 2),
                    InputAreaHeight
                );
            }
            else
            {
                return new Rect(
                    MARGIN,
                    WindowHeight - InputAreaHeight - MARGIN,
                    ChatAreaWidth,
                    InputAreaHeight
                );
            }
        }

        public Rect GetSendButtonRect()
        {
            if (IsPortrait)
            {
                float yPos = WindowHeight - InputAreaHeight - MARGIN;
                float xPos = WindowWidth - SendButtonWidth - MARGIN;
                return new Rect(
                    xPos,
                    yPos,
                    SendButtonWidth - MARGIN,
                    InputAreaHeight - MARGIN
                );
            }
            else
            {
                return new Rect(ChatAreaWidth - SendButtonWidth + MARGIN, 
                              WindowHeight - InputAreaHeight - MARGIN,
                              SendButtonWidth, InputAreaHeight);
            }
        }

        public Rect GetInputFieldRect()
        {
            if (IsPortrait)
            {
                return new Rect(MARGIN, WindowHeight - InputAreaHeight - MARGIN,
                              WindowWidth - SendButtonWidth - (MARGIN * 3), InputAreaHeight);
            }
            else
            {
                return new Rect(MARGIN, WindowHeight - InputAreaHeight - MARGIN,
                              ChatAreaWidth - SendButtonWidth - (MARGIN * 2), InputAreaHeight);
            }
        }

        // 添加獲取輸入框樣式的方法
        public GUIStyle GetInputFieldStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.textField);
            style.fontSize = (int)CurrentFontSize;
            return style;
        }

        // 修改獲取用戶按鈕位置的方法
        public Rect GetUserButtonRect(int index, int totalUsers)
        {
            if (IsPortrait)
            {
                float xPos = MARGIN + (UserButtonWidth + INNER_MARGIN/2) * index;  // 減少按鈕間距
                return new Rect(
                    xPos,
                    MARGIN/2,  // 減少頂部間距
                    UserButtonWidth,
                    UserListHeight - MARGIN
                );
            }
            return Rect.zero; // 橫向模式不使用此方法
        }
        #endregion
    }
}