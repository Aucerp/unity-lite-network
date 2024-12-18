using UnityEngine;
using System.Collections.Generic;
using Unet.LitUdp;

public class UI_UdpManager : MonoBehaviour
{
    private List<string> messageLog = new List<string>();
    private Vector2 scrollPosition;
    private string inputMessage = string.Empty;
    private const int MAX_LOG_COUNT = 100;
    private const float WINDOW_WIDTH = 400f;
    private const float WINDOW_HEIGHT = 300f;

    void Start()
    {
        // 訂閱事件
        UdpEventSystem.OnMessageReceived += HandleMessageReceived;
        UdpEventSystem.OnErrorOccurred += HandleError;
        UdpEventSystem.OnMessageSent += HandleMessageSent;
    }

    void OnDestroy()
    {
        // 取消訂閱事件
        UdpEventSystem.OnMessageReceived -= HandleMessageReceived;
        UdpEventSystem.OnErrorOccurred -= HandleError;
        UdpEventSystem.OnMessageSent -= HandleMessageSent;
    }

    private void HandleMessageReceived(string message)
    {
        AddToLog(string.Format("接收: {0}", message));
    }

    private void HandleError(System.Exception ex)
    {
        AddToLog(string.Format("錯誤: {0}", ex.Message));
    }

    private void HandleMessageSent(string message)
    {
        AddToLog(string.Format("發送: {0}", message));
    }

    private void AddToLog(string message)
    {
        messageLog.Add(string.Format("[{0}] {1}", System.DateTime.Now.ToString("HH:mm:ss"), message));
        if (messageLog.Count > MAX_LOG_COUNT)
        {
            messageLog.RemoveAt(0);
        }
    }

    void OnGUI()
    {
        // 主窗口
        GUILayout.Window(0, new Rect(
            Screen.width / 2 - WINDOW_WIDTH / 2,
            Screen.height / 2 - WINDOW_HEIGHT / 2,
            WINDOW_WIDTH,
            WINDOW_HEIGHT
        ), DrawWindow, "UDP 消息系統");
    }

    void DrawWindow(int windowID)
    {
        // 消息日誌區域
        GUILayout.BeginVertical();
        
        // 滾動視圖
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        foreach (string message in messageLog)
        {
            GUILayout.Label(message);
        }
        GUILayout.EndScrollView();

        // 輸入區域
        GUILayout.BeginHorizontal();
        inputMessage = GUILayout.TextField(inputMessage, GUILayout.Width(300));
        if (GUILayout.Button("發送", GUILayout.Width(60)))
        {
            if (!string.IsNullOrEmpty(inputMessage))
            {
                // 查找場景中的 UdpManager 並發送消息
                UdpManager manager = FindObjectOfType<UdpManager>();
                if (manager != null)
                {
                    manager.SendUdpMessage(inputMessage);
                    inputMessage = string.Empty;
                }
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }
} 