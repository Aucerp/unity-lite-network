# UDP Chat Room System Documentation

## Overview
A real-time chat room system implemented in Unity using UDP protocol, supporting host-client architecture with multiple clients.

## Features

### 1. Room System
- **Room Creation & Joining**
  ```csharp
  // Create a room as host
  chatManager.CreateRoom(username, port);
  
  // Join an existing room
  chatManager.JoinRoom(username, localPort, hostIP, hostPort);
  ```

- **Host Identification**
  - Host is marked in yellow with [房主] tag
  - Prevents multiple host issues
  - Auto-detection of host status

### 2. User Management
- **User List Features**
  - Real-time user list updates
  - Self marked in gray with (我) tag
  - Selected user highlighted in cyan
  - Automatic client disconnection when host leaves

- **User Status Display**
  ```csharp
  // User display format in list
  string displayName = user;
  if (isSelf) displayName += " (我)";
  if (isHost) displayName += " [房主]";
  ```

### 3. Message System
- **Message Types**
  ```csharp
  public enum MessageType
  {
      Chat,           // Regular chat message
      Join,           // Room join notification
      Leave,          // Room leave notification
      UserList,       // User list update
      Private         // Private message
  }
  ```

- **Messaging Features**
  - Broadcast messages to all users
  - Private messaging with sender's copy
  - System notifications
  - Error messages

### 4. Stability Features
- **Connection Management**
  - Reliable host-client communication
  - Automatic host disconnection detection
  - Graceful error handling

- **Error Handling**
  ```csharp
  try
  {
      // Operation code
  }
  catch (Exception ex)
  {
      Debug.LogError($"[Error] {ex.Message}");
      OnError?.Invoke($"Error: {ex.Message}");
  }
  ```

### 5. UI/UX Implementation
- **Main Components**
  - Login window
  - Chat window
  - User list
  - Message input area

- **Visual Feedback**
  ```csharp
  // Color coding for different user types
  if (isSelf) GUI.color = Color.gray;        // Self
  else if (isHost) GUI.color = Color.yellow; // Host
  else GUI.color = isSelected ? 
      Color.cyan : Color.white;              // Others
  ```

## Usage Example
