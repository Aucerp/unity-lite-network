using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System;
using System.Linq;

namespace Unet.LitUdp.Chat
{
    public class ChatManager : MonoBehaviour
    {
        private UdpSender sender;
        private UdpReceiver receiver;
        private string userName;
        private int localPort;
        private Dictionary<string, IPEndPoint> userEndPoints = new Dictionary<string, IPEndPoint>();
        private bool isHost;
        private int hostPort = 11000; // 默認主機端口
        private bool isInRoom;  // 添加 isInRoom 字段
        public string hostName { get; private set; }

        public event Action<ChatMessage> OnChatMessageReceived;
        public event Action<List<string>> OnUserListUpdated;
        public event Action<bool> OnRoomJoined;  // 連接成功事件
        public event Action<string> OnError;      // 錯誤事件

        public string UserName { get { return userName; } }
        public bool IsHost { get { return isHost; } }
        public bool IsInRoom { get { return isInRoom; } }  // 添加屬性
        public int HostPort { get { return hostPort; } }

        void OnDestroy()
        {
            LeaveChat();
            if (sender != null)
            {
                sender.Dispose();
            }
            if (receiver != null)
            {
                receiver.Dispose();
            }
        }

        // 創建房間（作為主機）
        public void CreateRoom(string username, int port)
        {
            try
            {
                this.userName = username;
                this.localPort = port;
                this.hostPort = port;
                this.isHost = true;
                this.isInRoom = true;
                this.hostName = username;  // 作為主機時，設置自己為主機名

                Debug.Log($"開始創建房間 - 用戶名: {username}, 端口: {port}");

                // 初始化接收器 - 主機需要監聽所有傳入連接
                receiver = new UdpReceiver(port);
                receiver.OnMessageReceived += HandleMessageReceived;
                receiver.Start();

                // 初始化發送器 - 主機初始化時不需要指定目標
                sender = new UdpSender("127.0.0.1", port);

                // 先觸發加入成功事件
                Debug.Log("房間創建成功，觸發加入事件");
                OnRoomJoined?.Invoke(true);

                // 然後添加自己到用戶列表並更新
                Debug.Log($"添加主機到用戶列表: {username}");
                userEndPoints[userName] = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
                UpdateUserList();

                Debug.Log($"房間創建完成，監聽端口: {port}");
            }
            catch (Exception ex)
            {
                this.isInRoom = false;
                this.hostName = null;  // 失敗時清除主機名
                Debug.LogError($"創建房間失敗: {ex.Message}");
                OnError?.Invoke("創建房間失敗: " + ex.Message);
            }
        }

        // 加入房間（作為客戶端）
        public void JoinRoom(string username, int localPort, string hostIp, int hostPort)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    throw new ArgumentException("用戶名不能為空");
                }

                if (string.IsNullOrEmpty(hostIp))
                {
                    throw new ArgumentException("主機 IP 不能為空");
                }

                if (hostPort <= 0)
                {
                    throw new ArgumentException("無效的主機端口");
                }

                this.userName = username;
                this.localPort = localPort;
                this.isHost = false;
                this.hostPort = hostPort;
                this.isInRoom = false;
                this.hostName = null;  // 加入時先清除主機名

                Debug.Log($"嘗試加入房間 - 用戶名: {username}, 本地端口: {localPort}, 主機IP: {hostIp}, 主機端口: {hostPort}");

                // 初始化接收器
                receiver = new UdpReceiver(localPort);
                receiver.OnMessageReceived += HandleMessageReceived;
                receiver.Start();

                // 初始化發送器（連接到主機）
                sender = new UdpSender(hostIp, hostPort);

                // 直接發送正確的加入請求，而不是測試數據
                SendJoinRequest();
            }
            catch (Exception ex)
            {
                this.isInRoom = false;
                this.hostName = null;
                Debug.LogError($"加入房間失敗: {ex.Message}");
                OnError?.Invoke("加入房間失敗: " + ex.Message);
            }
        }

        private void SendJoinRequest()
        {
            try
            {
                ChatMessage joinMessage = new ChatMessage
                {
                    FromName = userName,
                    Type = MessageType.Join,
                    FromPort = localPort,
                    Content = $"JoinRequest_{DateTime.Now.Ticks}",  // 添加時戳以避免重複
                    ToName = ""  // 發送給主機
                };

                string serialized = SerializeMessage(joinMessage);
                Debug.Log($"[客戶端] 發送加入請求消息: {serialized}");
                
                if (sender != null)
                {
                    sender.SendMessage(serialized);
                    Debug.Log("[客戶端] 加入請求已發送");

                    // 主機回應時會發送一個 Join 類型的消息，其中包含主機名稱
                    ChatMessage hostResponse = new ChatMessage
                    {
                        FromName = userName,  // 主機名稱
                        Type = MessageType.Join,
                        FromPort = hostPort,
                        Content = "HostResponse",
                        ToName = joinMessage.FromName
                    };

                    // 主機發送回應
                    if (isHost)
                    {
                        string responseJson = SerializeMessage(hostResponse);
                        sender.SendMessage(responseJson);
                        Debug.Log("[主機] 已發送主機身份回應");
                    }
                }
                else
                {
                    throw new InvalidOperationException("發送器未初始化");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"發送加入請求失敗: {ex.Message}");
                OnError?.Invoke("發送加入請求失敗: " + ex.Message);
            }
        }

        private void HandleMessageReceived(string data)
        {
            try
            {
                Debug.Log($"[消息接收] 開始處理原始數據: {data}");
                
                // 檢查是否是 V2 格式的消息
                if (data.Contains("FromId"))
                {
                    Debug.Log("[消息接收] 檢測到 V2 格式消息");
                    var v2Message = JsonUtility.FromJson<V2Message>(data);
                    
                    // 將 V2 消息轉換為 V1 格式
                    ChatMessage message = new ChatMessage
                    {
                        FromName = v2Message.Content,  // V2 中用戶名存在 Content 中
                        Type = MessageType.Join,
                        FromPort = 0,  // 暫時設為 0，後面會從 FromPort 獲取
                        Content = v2Message.Content,
                        ToName = ""
                    };

                    Debug.Log($"[消息接收] V2 消息轉換為 V1: FromName={message.FromName}, Type={message.Type}");

                    // 特別處理 Join 消息
                    if (isHost)
                    {
                        Debug.Log("[消息接收] 主機模式，處理加入請求");
                        HandleJoinMessage(message);
                        return;
                    }
                }
                else
                {
                    // 原有的 V1 消息處理邏輯
                    ChatMessage message = DeserializeMessage(data);
                    if (message == null)
                    {
                        Debug.LogError("[消息接收] 消息解析失敗");
                        return;
                    }

                    Debug.Log($"[消息接收] 消息類型={message.Type}, 來自={message.FromName}, 端口={message.FromPort}, 內容={message.Content}");

                    // 檢查是否主機
                    Debug.Log($"[消息接收] 當前是否為主機: {isHost}");

                    // 更新發送者的端點信息
                    if (!string.IsNullOrEmpty(message.FromName))
                    {
                        Debug.Log($"[消息接收] 開始理來自 {message.FromName} 的消息");
                        
                        if (!userEndPoints.ContainsKey(message.FromName))
                        {
                            IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), message.FromPort);
                            userEndPoints[message.FromName] = senderEndPoint;
                            Debug.Log($"[消息接收] 添加新用戶端點: {message.FromName}, 端口: {message.FromPort}");
                        }

                        switch (message.Type)
                        {
                            case MessageType.UserList:
                                Debug.Log("[消息接收] 處理用戶列表消息");
                                if (!isHost)
                                {
                                    HandleUserListMessage(message);
                                }
                                break;
                            case MessageType.Join:
                                Debug.Log("[消息接收] 處理加入消息");
                                HandleJoinMessage(message);
                                break;
                            case MessageType.Leave:
                                Debug.Log("[消息接收] 處理離開消息");
                                HandleLeaveMessage(message);
                                break;
                            case MessageType.Chat:
                            case MessageType.Private:
                                Debug.Log("[消息接收] 處理聊天消息");
                                OnChatMessageReceived?.Invoke(message);
                                break;
                            default:
                                Debug.Log($"[消息接收] 未知消息類型: {message.Type}");
                                break;
                        }
                    }
                    else
                    {
                        Debug.LogError("[消息接收] 消息中的用戶名為空");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[消息接收] 處理消息時發生錯誤: {ex.Message}\n{ex.StackTrace}");
                OnError?.Invoke($"消息處理錯誤: {ex.Message}");
            }
        }

        private void HandleJoinMessage(ChatMessage message)
        {
            Debug.Log($"[主機回覆] 開始處理加入請求 - 來自: {message.FromName}");
            
            if (isHost)
            {
                try
                {
                    Debug.Log($"[主機回覆] 確認身份是主機，開始處理");
                    
                    // 1. 先確保新用戶被添加到用戶列表中
                    if (!userEndPoints.ContainsKey(message.FromName))
                    {
                        userEndPoints[message.FromName] = new IPEndPoint(IPAddress.Parse("127.0.0.1"), message.FromPort);
                        Debug.Log($"[主機回覆] 新增用戶到列表 - 用戶名: {message.FromName}");
                    }

                    // 2. 立即發送歡迎消息
                    ChatMessage welcomeMessage = new ChatMessage
                    {
                        FromName = userName,
                        ToName = message.FromName,
                        Type = MessageType.Chat,
                        Content = "歡迎加入聊天室！",
                        FromPort = localPort
                    };

                    string welcomeJson = SerializeMessage(welcomeMessage);
                    Debug.Log($"[主機回覆] 準備發送歡迎消息: {welcomeJson}");

                    // 使用新用戶的端點信息發送消息
                    IPEndPoint newUserEndPoint = userEndPoints[message.FromName];
                    sender = new UdpSender(newUserEndPoint.Address.ToString(), newUserEndPoint.Port);
                    sender.SendMessage(welcomeJson);
                    Debug.Log($"[主機回覆] 已發送歡迎消息");

                    // 3. 發送用戶列表
                    SendUserListToUser(message.FromName);

                    // 4. 更新用戶列表
                    UpdateUserList();
                    Debug.Log($"[主機回覆] 處理加入請求完成");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[主機回覆] 錯誤：{ex.Message}\n{ex.StackTrace}");
                    OnError?.Invoke($"處理加入請求失敗：{ex.Message}");
                }
            }
            else
            {
                // 如果是客戶端收到主機的回應，記錄主機名稱
                Debug.Log($"[客戶端] 收到主機回應，記錄主機名稱: {message.FromName}");
                hostName = message.FromName;
            }
        }

        private void HandleLeaveMessage(ChatMessage message)
        {
            try
            {
                Debug.Log($"[處理離開] 收到用戶 {message.FromName} 的離開消息: {message.Content}");

                // 從用戶列表中移除
                userEndPoints.Remove(message.FromName);
                
                // 如果是主機離開
                if (message.FromName == hostName)
                {
                    Debug.Log("[處理離開] 主機離開房間，客戶端將關閉");
                    isInRoom = false;
                    userEndPoints.Clear();
                    
                    // 添加系統消息
                    OnChatMessageReceived?.Invoke(new ChatMessage
                    {
                        FromName = "系統",
                        Content = "主機已關閉房間",
                        Type = MessageType.Chat,
                        Timestamp = DateTime.Now.Ticks
                    });

                    // 確保觸發房間關閉事件
                    OnRoomJoined?.Invoke(false);

                    // 主動清理資源
                    if (sender != null)
                    {
                        sender.Dispose();
                        sender = null;
                    }
                    if (receiver != null)
                    {
                        receiver.Dispose();
                        receiver = null;
                    }
                }
                else
                {
                    // 更新用戶列表
                    UpdateUserList();
                    // 廣播系統消息
                    if (isHost)
                    {
                        BroadcastChatMessage($"系統: {message.FromName} 離開了聊天室");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[處理離開] 處理離開消息時發生錯誤: {ex.Message}");
            }
        }

        private void HandleUserListMessage(ChatMessage message)
        {
            try
            {
                Debug.Log($"[客戶端] 收到用戶列表據: {message.Content}");
                var userList = JsonUtility.FromJson<UserListData>(message.Content);
                
                if (userList == null || userList.Users == null)
                {
                    Debug.LogError("[客戶端] 用戶列表數據無效");
                    return;
                }

                Debug.Log($"[客戶端] 解析到 {userList.Users.Length} 個用戶");
                
                // 更��用戶列表和主機信息
                userEndPoints.Clear();
                string foundHost = null;
                foreach (var user in userList.Users)
                {
                    Debug.Log($"[客戶端] 添加用戶到列表: {user.Name}, IP: {user.IP}, Port: {user.Port}, IsHost: {user.IsHost}");
                    userEndPoints[user.Name] = new IPEndPoint(IPAddress.Parse(user.IP), user.Port);
                    
                    // 如果找到主機，記錄主機名稱
                    if (user.IsHost)
                    {
                        if (foundHost != null)
                        {
                            Debug.LogWarning($"[客戶端] 檢測到多個主機: {foundHost} 和 {user.Name}");
                            continue;  // 跳過額外的主機標記
                        }
                        Debug.Log($"[客戶端] 發現主機用戶: {user.Name}");
                        foundHost = user.Name;
                    }
                }

                // 只有在確實找到唯一主機時才更新主機名
                if (foundHost != null)
                {
                    hostName = foundHost;
                }
                else if (!isHost)  // 如果是客戶端但找不到主機
                {
                    Debug.LogWarning("[客戶端] 用戶列表中未找到主機標記");
                }

                // 確保自己也在用戶列表中
                if (!userEndPoints.ContainsKey(userName))
                {
                    Debug.Log($"[客戶端] 添加自己到用戶列表: {userName}, Port: {localPort}");
                    userEndPoints[userName] = new IPEndPoint(IPAddress.Parse("127.0.0.1"), localPort);
                }

                var currentUsers = new List<string>(userEndPoints.Keys);
                Debug.Log($"[客戶端] 觸發用戶列表更新事件，當前用戶: {string.Join(", ", currentUsers)}，主機: {hostName}");
                OnUserListUpdated?.Invoke(currentUsers);

                // 如果是第一次收到用戶列表，觸發加入成功事件
                if (!isInRoom)
                {
                    Debug.Log("[客戶端] 首次收到用戶列表，觸發加入成功事件");
                    isInRoom = true;
                    OnRoomJoined?.Invoke(true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[客戶端] 處理用戶列表失敗: {ex.Message}");
                OnError?.Invoke("處理用戶列表失敗: " + ex.Message);
            }
        }

        private void SendUserListToUser(string targetUser)
        {
            try
            {
                Debug.Log($"[主機回覆] SendUserListToUser - 開始為 {targetUser} 準備用戶列表");
                
                var userListData = new UserListData
                {
                    Users = userEndPoints.Select(kvp => new UserData 
                    { 
                        Name = kvp.Key,
                        IP = kvp.Value.Address.ToString(),
                        Port = kvp.Value.Port,
                        IsHost = kvp.Key == userName  // 添加主機標識
                    }).ToArray()
                };

                string userListJson = JsonUtility.ToJson(userListData);
                Debug.Log($"[主機回覆] 準備發送的用戶列表數據: {userListJson}");

                ChatMessage message = new ChatMessage
                {
                    FromName = userName,
                    ToName = targetUser,
                    Type = MessageType.UserList,
                    Content = userListJson,
                    FromPort = localPort
                };

                IPEndPoint targetEndPoint = userEndPoints[targetUser];
                Debug.Log($"[主機回覆] 發送用戶列表到端點 - IP: {targetEndPoint.Address}, 端口: {targetEndPoint.Port}");
                
                sender = new UdpSender(targetEndPoint.Address.ToString(), targetEndPoint.Port);
                string serializedMessage = SerializeMessage(message);
                sender.SendMessage(serializedMessage);
                Debug.Log($"[主機回覆] 用戶列表發送完成 - 序列化後的消息: {serializedMessage}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[主機回覆] 發送用戶列表失敗: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void UpdateUserList()
        {
            try
            {
                var users = new List<string>(userEndPoints.Keys);
                Debug.Log($"觸發用戶列表更新事件，當前用戶列表: [{string.Join(", ", users)}]");
                OnUserListUpdated?.Invoke(users);
            }
            catch (Exception ex)
            {
                Debug.LogError($"更新用戶列表失敗: {ex.Message}");
            }
        }

        public void SendPrivateMessage(string toUser, string content, MessageType type = MessageType.Private)
        {
            try
            {
                if (!userEndPoints.ContainsKey(toUser))
                {
                    Debug.LogError($"找不到用戶 {toUser} 的端點信息");
                    return;
                }

                ChatMessage message = new ChatMessage
                {
                    FromName = userName,
                    ToName = toUser,
                    Content = content,
                    Type = type,
                    FromPort = localPort
                };

                // 發送給目標用戶
                IPEndPoint targetEndPoint = userEndPoints[toUser];
                Debug.Log($"發送私人消息到 {toUser}，IP：{targetEndPoint.Address}，端口：{targetEndPoint.Port}");
                
                sender = new UdpSender(targetEndPoint.Address.ToString(), targetEndPoint.Port);
                string serializedMessage = SerializeMessage(message);
                sender.SendMessage(serializedMessage);
                
                Debug.Log($"已發送消息：{serializedMessage}");

                // 新增：觸發本地消息接收事件，這樣發送者也能看到自己發送的私聊消息
                OnChatMessageReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"發送私人消息失敗：{ex.Message}");
            }
        }

        public void BroadcastChatMessage(string content)
        {
            ChatMessage message = new ChatMessage
            {
                FromName = userName,
                ToName = "",
                Content = content,
                Type = MessageType.Chat,
                FromPort = localPort
            };

            foreach (var endpoint in userEndPoints.Values)
            {
                sender = new UdpSender(endpoint.Address.ToString(), endpoint.Port);
                sender.SendMessage(SerializeMessage(message));
            }
        }

        public void LeaveChat()
        {
            if (string.IsNullOrEmpty(userName)) return;

            try
            {
                Debug.Log($"[離開房間] 用戶 {userName} 開始離開房間");

                ChatMessage message = new ChatMessage
                {
                    FromName = userName,
                    Type = MessageType.Leave,
                    FromPort = localPort,
                    Content = isHost ? "主機關閉房間" : "用戶離開房間",
                    ToName = ""
                };

                // 如果是主機，通知所有用戶主機關閉
                if (isHost)
                {
                    Debug.Log("[離開房間] 主機關閉房間，通知所有用戶");
                    foreach (var endpoint in userEndPoints.Values)
                    {
                        sender = new UdpSender(endpoint.Address.ToString(), endpoint.Port);
                        sender.SendMessage(SerializeMessage(message));
                    }
                }
                else if (sender != null) // 如果是客戶端，只需通知主機
                {
                    Debug.Log("[離開房間] 客戶端離開，通知主機");
                    sender = new UdpSender("127.0.0.1", hostPort);
                    sender.SendMessage(SerializeMessage(message));
                }

                isInRoom = false;
                userEndPoints.Clear();
                hostName = null;  // 清除主機名
            }
            catch (Exception ex)
            {
                Debug.LogError($"[離開房間] 發送離開消息時發生錯誤: {ex.Message}");
            }
        }

        private string SerializeMessage(ChatMessage message)
        {
            try
            {
                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }
                string json = JsonUtility.ToJson(message);
                Debug.Log($"序列化消息: {json}");
                return json;
            }
            catch (Exception ex)
            {
                Debug.LogError($"序列化消息失敗: {ex.Message}");
                throw;
            }
        }

        private ChatMessage DeserializeMessage(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                {
                    Debug.LogError("反序列化失敗：數據為空");
                    return null;
                }

                Debug.Log($"嘗試反序列化數據: {data}");
                ChatMessage message = JsonUtility.FromJson<ChatMessage>(data);
                
                if (message == null)
                {
                    Debug.LogError("反序列化結果為空");
                    return null;
                }

                return message;
            }
            catch (Exception ex)
            {
                Debug.LogError($"反序列化失敗: {ex.Message}, 數據: {data}");
                return null;
            }
        }
    }

    [Serializable]
    public class UserListData
    {
        public UserData[] Users;
    }

    [Serializable]
    public class UserData
    {
        public string Name;
        public string IP;
        public int Port;
        public bool IsHost;  // 添加主機標識
    }

    [Serializable]
    public class V2Message
    {
        public string FromId;
        public string Content;
        public string Type;
        public int FromPort;
    }
} 