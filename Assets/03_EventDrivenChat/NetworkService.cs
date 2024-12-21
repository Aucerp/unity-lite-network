using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unet.EventDrivenChat.MessagesHandler;

namespace Unet.EventDrivenChat
{
    public class NetworkService : IDisposable
    {
        private readonly MessagesHandler.MessageHandlerRegistry messageHandlerRegistry;
        private UdpClient udpClient;
        private CancellationTokenSource cancellationTokenSource;
        private Task receiveTask;
        private string localIP;

        public string LocalIP => localIP;

        public NetworkService(MessagesHandler.MessageHandlerRegistry registry)
        {
            messageHandlerRegistry = registry;
            InitializeLocalIP();
        }

        private void InitializeLocalIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                        return;
                    }
                }
                localIP = "127.0.0.1";
            }
            catch (Exception ex)
            {
                Debug.LogError($"獲取本機IP失敗: {ex.Message}");
                localIP = "127.0.0.1";
            }
        }

        public void StartListening(int port)
        {
            try
            {
                StopListening();

                udpClient = new UdpClient(port);
                cancellationTokenSource = new CancellationTokenSource();
                receiveTask = Task.Run(ReceiveLoop, cancellationTokenSource.Token);

                Debug.Log($"開始在端口 {port} 監聽");
            }
            catch (Exception ex)
            {
                Debug.LogError($"啟動監聽失敗: {ex.Message}");
                throw;
            }
        }

        private async Task ReceiveLoop()
        {
            while (!cancellationTokenSource?.Token.IsCancellationRequested ?? true)
            {
                try
                {
                    if (udpClient == null) break;

                    var result = await udpClient.ReceiveAsync()
                        .ConfigureAwait(false);  // 避免死鎖

                    if (cancellationTokenSource?.Token.IsCancellationRequested ?? true)
                    {
                        break;
                    }

                    string json = Encoding.UTF8.GetString(result.Buffer);
                    Debug.Log($"收到原始消息: {json}");
                    
                    var message = JsonUtility.FromJson<ChatMessage>(json);
                    
                    if (message != null)
                    {
                        message.FromIP = result.RemoteEndPoint.Address.ToString();
                        Debug.Log($"解析消息: Type={message.Type}, From={message.FromName}, Content={message.Content}");
                        
                        // 使用 try-catch 包裝 UI 更新
                        try
                        {
                            await UnityMainThreadDispatcher.Instance.EnqueueAsync(() => 
                            {
                                if (!cancellationTokenSource?.Token.IsCancellationRequested ?? true)
                                {
                                    Debug.Log($"準備處理消息: {message.Content}");
                                    messageHandlerRegistry.HandleMessage(message);
                                    Debug.Log($"消息處理完成: {message.Content}");
                                }
                            }).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"UI更新時發生錯誤: {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"消息解析失敗: {json}");
                    }
                }
                catch (ObjectDisposedException)
                {
                    // UDP客戶端被關閉，觸發錯誤事件
                    ChatEvents.RaiseError("連線失敗");
                    break;
                }
                catch (OperationCanceledException)
                {
                    // 操作被取消，觸發錯誤事件
                    ChatEvents.RaiseError("連線失敗");
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationTokenSource?.Token.IsCancellationRequested ?? true)
                    {
                        Debug.LogError($"接收消息時發生錯誤: {ex.Message}");
                        ChatEvents.RaiseError("連線失敗");
                    }
                }
            }
        }

        public void SendNetworkMessage(ChatMessage message, string ip, int port)
        {
            try
            {
                if (udpClient == null)
                {
                    Debug.LogError("UDP客戶端為空，無法發送消息");
                    return;
                }

                string json = JsonUtility.ToJson(message);
                Debug.Log($"準備發送JSON: {json} 到 {ip}:{port}");
                byte[] data = Encoding.UTF8.GetBytes(json);
                
                // 使用 BeginSend 異步發送
                udpClient.BeginSend(data, data.Length, ip, port, ar =>
                {
                    try
                    {
                        int bytesSent = udpClient?.EndSend(ar) ?? 0;
                        Debug.Log($"成功發送 {bytesSent} 字節到 {ip}:{port}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"完成發送時發生錯誤: {ex.Message}");
                    }
                }, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"發送消息失敗: {ex.Message}");
                throw;
            }
        }

        private void StopListening()
        {
            try
            {
                // 先取消令牌
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource = null;
                }

                // 關閉 UDP 客戶端，這會導致所有待處理的操作拋出異常
                if (udpClient != null)
                {
                    udpClient.Close();
                    udpClient.Dispose();
                    udpClient = null;
                }

                // 不等待 receiveTask，讓它自然結束
                receiveTask = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"停止監聽時發生錯誤: {ex.Message}");
                // 觸發錯誤事件
                ChatEvents.RaiseError("連線失敗");
            }
        }

        public void Dispose()
        {
            StopListening();
        }
    }
} 