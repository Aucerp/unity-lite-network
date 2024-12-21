using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using Unet.EventDrivenChat.MessagesHandler;
using System.Threading.Tasks;

namespace Unet.EventDrivenChat
{
    public class UserInfo
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
    }

    public class ChatRoom : IDisposable
    {
        private readonly NetworkService networkService;
        private readonly MessagesHandler.MessageHandlerRegistry messageHandlerRegistry;
        private readonly ConcurrentDictionary<string, UserInfo> users = 
            new ConcurrentDictionary<string, UserInfo>();

        private string userName;
        private int localPort;
        private bool isHost;
        private bool isInRoom;
        private string hostName;
        private string hostIP;
        private int hostPort;

        public bool IsHost => isHost;
        public bool IsInRoom => isInRoom;
        public string UserName => userName;
        public string HostName => hostName;
        public int LocalPort => localPort;
        public int HostPort => hostPort;
        public string LocalIP => networkService.LocalIP;

        public ChatRoom()
        {
            messageHandlerRegistry = new MessagesHandler.MessageHandlerRegistry();
            networkService = new NetworkService(messageHandlerRegistry);
            RegisterMessageHandlers();
        }

        private void RegisterMessageHandlers()
        {
            messageHandlerRegistry.RegisterHandler(new ChatMessageHandler(this));
            messageHandlerRegistry.RegisterHandler(new JoinMessageHandler(this));
            messageHandlerRegistry.RegisterHandler(new LeaveMessageHandler(this));
            messageHandlerRegistry.RegisterHandler(new UserListMessageHandler(this));
            messageHandlerRegistry.RegisterHandler(new PrivateMessageHandler(this));
            messageHandlerRegistry.RegisterHandler(new SystemMessageHandler(this));
        }

        public void CreateRoom(string username, int port)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    throw new ArgumentException("用戶名不能為空");
                }

                this.userName = username;
                this.localPort = port;
                this.hostPort = port;
                this.isHost = true;
                this.hostName = username;
                this.hostIP = LocalIP;

                networkService.StartListening(localPort);
                AddUser(username, LocalIP, localPort);
                isInRoom = true;

                ChatEvents.RaiseRoomStateChanged(true);
                ChatEvents.RaiseSystemMessage($"房間已創建，端口: {localPort}");
                ChatEvents.RaiseUserListUpdated(GetUserList());
            }
            catch (Exception ex)
            {
                HandleError($"創建房間失敗: {ex.Message}");
            }
        }

        public void JoinRoom(string username, int localPort, string hostIP, int hostPort)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    throw new ArgumentException("用戶名不能為空");
                }

                this.userName = username;
                this.localPort = localPort;
                this.hostPort = hostPort;
                this.hostIP = hostIP;

                // 清理舊的連接
                users.Clear();
                
                // 添加自己到用戶列表
                AddUser(username, LocalIP, localPort);

                networkService.StartListening(localPort);

                var joinMessage = new ChatMessage
                {
                    FromName = username,
                    Type = MessageType.Join,
                    FromPort = localPort,
                    FromIP = LocalIP
                };

                SendNetworkDirectMessage(joinMessage, hostIP, hostPort);
                ChatEvents.RaiseSystemMessage($"正在加入房間 {hostIP}:{hostPort}...");
            }
            catch (Exception ex)
            {
                HandleError($"加入房間失敗: {ex.Message}");
            }
        }

        public void LeaveRoom()
        {
            if (!isInRoom) return;

            try
            {
                var leaveMessage = new ChatMessage
                {
                    FromName = userName,
                    Type = MessageType.Leave,
                    FromPort = localPort,
                    FromIP = LocalIP,
                    Content = isHost ? "主機關閉房間" : "用戶離開房間"
                };

                isInRoom = false;

                if (isHost)
                {
                    foreach (var user in users.Values.ToList())
                    {
                        if (user.Name != userName)
                        {
                            SendNetworkDirectMessage(leaveMessage, user.IP, user.Port);
                        }
                    }
                }
                else if (hostName != null)
                {
                    SendNetworkDirectMessage(leaveMessage, hostIP, hostPort);
                }

                CleanupRoom();
                Debug.Log($"已離開房間: {userName}");
            }
            catch (Exception ex)
            {
                HandleError($"離開房間失敗: {ex.Message}");
            }
            finally
            {
                try
                {
                    networkService?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"清理網絡服務時發生錯誤: {ex.Message}");
                }
            }
        }

        private void CleanupRoom()
        {
            isInRoom = false;
            users.Clear();
            hostName = null;
            hostIP = null;
            hostPort = 0;
            isHost = false;
            localPort = 0;

            ChatEvents.RaiseRoomStateChanged(false);
            ChatEvents.RaiseUserListUpdated(new List<string>());
            //ChatEvents.RaiseSystemMessage("已離開房間");
        }

        public void SendChatMessage(string content, string targetUser = null)
        {
            if (!isInRoom)
            {
                Debug.LogError("未在房間中，無法發送消息");
                return;
            }

            try
            {   
                var message = new ChatMessage
                {
                    FromName = userName,
                    ToName = targetUser,
                    Content = content,
                    Type = targetUser != null ? MessageType.Private : MessageType.Chat,
                    FromPort = localPort,
                    FromIP = LocalIP
                };

                // 先在本地處理消息
                messageHandlerRegistry.HandleMessage(message);

                if (targetUser != null)
                {
                    // 私聊消息
                    if (users.TryGetValue(targetUser, out UserInfo targetInfo))
                    {
                        Debug.Log($"發送私聊消息給 {targetUser}");
                        SendNetworkDirectMessage(message, targetInfo.IP, targetInfo.Port);
                    }
                }
                else
                {
                    Debug.Log($"發送群聊消息: {content}");
                    
                    if (isHost)
                    {
                        // 主機廣播給所有其他用戶
                        Debug.Log("作為主機發送消息給所有客戶端");
                        foreach (var user in users.Values)
                        {
                            if (user.Name != userName)  // 不需要發給自己
                            {
                                Debug.Log($"發送給客戶端: {user.Name}");
                                SendNetworkDirectMessage(message, user.IP, user.Port);
                            }
                        }
                    }
                    else
                    {
                        // 客戶端發送給主機，由主機轉發
                        Debug.Log("作為客戶端發送消息給主機");
                        SendNetworkDirectMessage(message, hostIP, hostPort);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError($"發送消息失敗: {ex.Message}");
                Debug.LogError($"發送消息時發生錯誤: {ex}");
            }
        }

        public void BroadcastMessage(ChatMessage message)
        {
            try
            {
                Debug.Log($"[BroadcastMessage] 開始廣播消息: {message.Content}");
                
                // 先在本地處理消息
                messageHandlerRegistry.HandleMessage(message);

                // 如果是主機，則廣播給所有客戶端
                if (isHost)
                {
                    Debug.Log("[BroadcastMessage] 主機廣播給所有客戶端");
                    foreach (var user in users.Values)
                    {
                        if (user.Name != userName)  // 不發給自己
                        {
                            Debug.Log($"[BroadcastMessage] 發送給: {user.Name}");
                            SendNetworkDirectMessage(message, user.IP, user.Port);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BroadcastMessage] 廣播消息時發生錯誤: {ex.Message}");
                HandleError($"廣播消息失敗: {ex.Message}");
            }
        }

        public void SendNetworkDirectMessage(ChatMessage message, string ip, int port)
        {
            networkService.SendNetworkMessage(message, ip, port);
        }

        public void AddUser(string name, string ip, int port)
        {
            users[name] = new UserInfo { Name = name, IP = ip, Port = port };
        }

        public void RemoveUser(string name)
        {
            users.TryRemove(name, out _);
            ChatEvents.RaiseUserListUpdated(GetUserList());
        }

        public List<string> GetUserList()
        {
            return users.Keys.ToList();
        }

        public void BroadcastUserList()
        {
            var message = new ChatMessage
            {
                FromName = userName,
                Type = MessageType.UserList,
                Content = string.Join(",", GetUserList()),
                FromPort = localPort,
                FromIP = LocalIP
            };
            BroadcastMessage(message);
        }

        public void SetHostName(string name)
        {
            hostName = name;
        }

        public void SetInRoom(bool value)
        {
            isInRoom = value;
        }

        private void HandleError(string error)
        {
            Debug.LogError(error);
            ChatEvents.RaiseError(error);
        }

        public void Dispose()
        {
            if (isInRoom)
            {
                LeaveRoom();
            }
        }

        public UserInfo GetUserInfo(string name)
        {
            if (users.TryGetValue(name, out var userInfo))
            {
                return userInfo;
            }
            return null;
        }
    }
} 
