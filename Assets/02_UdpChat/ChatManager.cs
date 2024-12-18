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
                this.isInRoom = true;  // 設置房間狀態

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
                this.isInRoom = false;  // 失敗時重置狀態
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
                this.isInRoom = false;  // 初始設置為 false，等待確認後再設為 true

                Debug.Log($"嘗試加入房間 - 用戶名: {username}, 本地端口: {localPort}, 主機IP: {hostIp}, 主機端口: {hostPort}");

                // 初始化接收器
                receiver = new UdpReceiver(localPort);
                receiver.OnMessageReceived += HandleMessageReceived;
                receiver.Start();

                // 初始化發送器（連接到主機）
                sender = new UdpSender(hostIp, hostPort);

                // 發送加入請求
                SendJoinRequest();
            }
            catch (Exception ex)
            {
                this.isInRoom = false;
                Debug.LogError($"加入房間失敗: {ex.Message}");
                OnError?.Invoke("加入房間失敗: " + ex.Message);
            }
        }

        private void SendJoinRequest()
        {
            try
            {
                ChatMessage message = new ChatMessage
                {
                    FromName = userName,
                    Type = MessageType.Join,
                    FromPort = localPort,
                    Content = "Request to join",  // 添加內容，避免空值
                    ToName = ""  // 明確設置為空字符串
                };

                string serialized = SerializeMessage(message);
                Debug.Log($"發送加入請求: {serialized}");
                sender.SendMessage(serialized);
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
                Debug.Log($"收到原始數據: {data}");
                
                ChatMessage message = DeserializeMessage(data);
                if (message == null)
                {
                    Debug.LogError("消息解析失敗");
                    return;
                }

                Debug.Log($"收到消息 - 類型: {message.Type}, 來自: {message.FromName}, 端口: {message.FromPort}");

                // 更新發送者的端點信息
                if (!string.IsNullOrEmpty(message.FromName))
                {
                    if (!userEndPoints.ContainsKey(message.FromName))
                    {
                        IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), message.FromPort);
                        userEndPoints[message.FromName] = senderEndPoint;
                        Debug.Log($"添加新用戶: {message.FromName}, 端口: {message.FromPort}");
                    }

                    switch (message.Type)
                    {
                        case MessageType.UserList:
                            if (!isHost)
                            {
                                HandleUserListMessage(message);
                                // 收到並處理完用戶列表後，再觸發加入成功事件
                                OnRoomJoined?.Invoke(true);
                            }
                            break;
                        case MessageType.Join:
                            HandleJoinMessage(message);
                            break;
                        case MessageType.Leave:
                            HandleLeaveMessage(message);
                            break;
                        case MessageType.Chat:
                        case MessageType.Private:
                            OnChatMessageReceived?.Invoke(message);
                            break;
                    }
                }
                else
                {
                    Debug.LogError("消息中的用戶名為空");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"消息處理錯誤: {ex.Message}");
                OnError?.Invoke("消息處理錯誤: " + ex.Message);
            }
        }

        private void HandleJoinMessage(ChatMessage message)
        {
            Debug.Log(string.Format("處理加入請求 - 來自: {0}, 端口: {1}", message.FromName, message.FromPort));
            
            if (isHost)
            {
                // 主機回應新戶
                SendPrivateMessage(message.FromName, "歡迎加入聊天室！", MessageType.Chat);
                
                // 發送當前用戶列表給新用戶
                SendUserListToUser(message.FromName);
                
                // 廣播新用戶加入的消息
                BroadcastChatMessage(string.Format("{0} 加入了聊天室", message.FromName));
            }
            else
            {
                // 客戶端收到主機的歡迎消息時，觸發加入成功事件
                OnRoomJoined?.Invoke(true);
            }
            
            UpdateUserList();
        }

        private void HandleLeaveMessage(ChatMessage message)
        {
            userEndPoints.Remove(message.FromName);
            UpdateUserList();
            if (isHost)
            {
                BroadcastChatMessage(string.Format("{0} 離開了聊天室", message.FromName));
            }
        }

        private void HandleUserListMessage(ChatMessage message)
        {
            try
            {
                Debug.Log($"[客戶端] 收到用戶列表數據: {message.Content}");
                var userList = JsonUtility.FromJson<UserListData>(message.Content);
                
                if (userList == null || userList.Users == null)
                {
                    Debug.LogError("[客戶端] 用戶列表數據無效");
                    return;
                }

                Debug.Log($"[客戶端] 解析到 {userList.Users.Length} 個用戶");
                
                userEndPoints.Clear();
                foreach (var user in userList.Users)
                {
                    Debug.Log($"[客戶端] 添加用戶到列表: {user.Name}, IP: {user.IP}, Port: {user.Port}");
                    userEndPoints[user.Name] = new IPEndPoint(IPAddress.Parse(user.IP), user.Port);
                }

                // 確保自己也在用戶列表中
                if (!userEndPoints.ContainsKey(userName))
                {
                    Debug.Log($"[客戶端] 添加自己到用戶列表: {userName}, Port: {localPort}");
                    userEndPoints[userName] = new IPEndPoint(IPAddress.Parse("127.0.0.1"), localPort);
                }

                var currentUsers = new List<string>(userEndPoints.Keys);
                Debug.Log($"[客戶端] 觸發用戶列表更新事件，當前用戶: {string.Join(", ", currentUsers)}");
                OnUserListUpdated?.Invoke(currentUsers);

                // 如果是第一次收到用戶列表，觸發加入成功事件
                if (!isInRoom)
                {
                    Debug.Log("[客戶端] 首次收到用戶列表，觸發加入成功事件");
                    isInRoom = true;  // 設置房間狀態
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
                var userListData = new UserListData
                {
                    Users = userEndPoints.Select(kvp => new UserData 
                    { 
                        Name = kvp.Key,
                        IP = kvp.Value.Address.ToString(),
                        Port = kvp.Value.Port
                    }).ToArray()
                };

                string userListJson = JsonUtility.ToJson(userListData);
                Debug.Log($"發送用戶列表到 {targetUser}: {userListJson}");

                ChatMessage message = new ChatMessage
                {
                    FromName = userName,
                    ToName = targetUser,
                    Type = MessageType.UserList,
                    Content = userListJson,
                    FromPort = localPort
                };

                // 直接使用 SendMessage 而��是 SendPrivateMessage
                IPEndPoint targetEndPoint = userEndPoints[targetUser];
                sender = new UdpSender(targetEndPoint.Address.ToString(), targetEndPoint.Port);
                sender.SendMessage(SerializeMessage(message));
            }
            catch (Exception ex)
            {
                Debug.LogError($"發送用戶列表失敗: {ex.Message}");
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
            if (!userEndPoints.ContainsKey(toUser)) return;

            ChatMessage message = new ChatMessage
            {
                FromName = userName,
                ToName = toUser,
                Content = content,
                Type = type,
                FromPort = localPort
            };

            IPEndPoint targetEndPoint = userEndPoints[toUser];
            sender = new UdpSender(targetEndPoint.Address.ToString(), targetEndPoint.Port);
            sender.SendMessage(SerializeMessage(message));
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

            ChatMessage message = new ChatMessage
            {
                FromName = userName,
                Type = MessageType.Leave,
                FromPort = localPort
            };

            if (sender != null)
            {
                sender.SendMessage(SerializeMessage(message));
            }

            isInRoom = false;  // 離開房間時設置狀態
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
    }
} 