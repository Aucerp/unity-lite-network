using UnityEngine;
using Unet.LitUdp;

public class UdpManager : MonoBehaviour
{
    private UdpSender sender;
    private UdpReceiver receiver;

    void Start()
    {
        // 初始化發送者和接收者
        sender = new UdpSender("127.0.0.1", 11000);
        receiver = new UdpReceiver(11000);

        // 設置接收消息的回調
        receiver.OnMessageReceived += OnMessageReceived;
        receiver.OnError += OnError;

        // 啟動接收器
        receiver.Start();
    }

    private void OnMessageReceived(string message)
    {
        Debug.Log(string.Format("收到消息: {0}", message));
        UdpEventSystem.RaiseMessageReceived(message);
    }

    private void OnError(System.Exception ex)
    {
        Debug.LogError(string.Format("發生錯誤: {0}", ex.Message));
        UdpEventSystem.RaiseErrorOccurred(ex);
    }

    public void SendUdpMessage(string message)
    {
        if (sender != null)
        {
            sender.SendMessage(message);
            UdpEventSystem.RaiseMessageSent(message);
        }
    }

    void Update()
    {
        // 測試發送消息
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendUdpMessage("Hello from UDP!");
        }
    }

    void OnDestroy()
    {
        // 清理資源
        if (sender != null)
        {
            sender.Dispose();
        }
        if (receiver != null)
        {
            receiver.Dispose();
        }
    }
} 