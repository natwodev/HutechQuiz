using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace backend_manage.Hubs;

public class NotificationHub : Hub
{
    // Lưu trữ thông tin kết nối của từng user
    private static readonly ConcurrentDictionary<string, UserConnectionInfo> _userConnections = new();

    public class UserConnectionInfo
    {
        public string ConnectionId { get; set; }
        public string UserId { get; set; }
        public string UserType { get; set; } // "student", "lecturer", "admin"
        public int? ExamRoomId { get; set; }
        public string StudentCode { get; set; }
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task JoinRoom(int examRoomId, string userId, string userType, string studentCode = null)
    {
        // Thêm user vào group phòng thi
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{examRoomId}");
        
        // Lưu thông tin kết nối
        var userInfo = new UserConnectionInfo
        {
            ConnectionId = Context.ConnectionId,
            UserId = userId,
            UserType = userType,
            ExamRoomId = examRoomId,
            StudentCode = studentCode
        };
        
        _userConnections.TryAdd(Context.ConnectionId, userInfo);
        
        // Thông báo cho các user khác trong phòng biết có user mới vào
        await Clients.Group($"room_{examRoomId}").SendAsync("UserJoinedRoom", new
        {
            userId = userId,
            userType = userType,
            studentCode = studentCode,
            connectionId = Context.ConnectionId
        });
    }

    public async Task LeaveRoom(int examRoomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{examRoomId}");
        
        // Xóa thông tin kết nối
        _userConnections.TryRemove(Context.ConnectionId, out _);
        
        // Thông báo cho các user khác trong phòng biết có user rời đi
        await Clients.Group($"room_{examRoomId}").SendAsync("UserLeftRoom", new
        {
            connectionId = Context.ConnectionId
        });
    }

    public async Task SendRoomStatusUpdate(int examRoomId, object data)
    {
        await Clients.Group($"room_{examRoomId}").SendAsync("RoomStatusUpdated", data);
    }

    // Phương thức để logout toàn bộ sinh viên trong phòng thi
    public async Task LogoutAllStudentsInRoom(int examRoomId, string reason = "Admin logout")
    {
        // Lấy tất cả connection của sinh viên trong phòng thi
        var studentConnections = _userConnections.Values
            .Where(u => u.ExamRoomId == examRoomId && u.UserType == "student")
            .ToList();

        // Gửi thông báo logout cho tất cả sinh viên trong phòng
        await Clients.Group($"room_{examRoomId}").SendAsync("ForceLogout", new
        {
            reason = reason,
            examRoomId = examRoomId,
            timestamp = DateTime.UtcNow
        });

        // Đóng kết nối của tất cả sinh viên trong phòng
        foreach (var connection in studentConnections)
        {
            await Clients.Client(connection.ConnectionId).SendAsync("Disconnect", new
            {
                reason = reason,
                message = "Bạn đã bị logout khỏi hệ thống thi"
            });
        }
    }

    // Phương thức để logout một sinh viên cụ thể
    public async Task LogoutStudent(string studentCode, int examRoomId, string reason = "Admin logout")
    {
        var studentConnection = _userConnections.Values
            .FirstOrDefault(u => u.StudentCode == studentCode && u.ExamRoomId == examRoomId && u.UserType == "student");

        if (studentConnection != null)
        {
            await Clients.Client(studentConnection.ConnectionId).SendAsync("ForceLogout", new
            {
                reason = reason,
                examRoomId = examRoomId,
                studentCode = studentCode,
                timestamp = DateTime.UtcNow
            });

            await Clients.Client(studentConnection.ConnectionId).SendAsync("Disconnect", new
            {
                reason = reason,
                message = "Bạn đã bị logout khỏi hệ thống thi"
            });
        }
    }

    // Phương thức để lấy danh sách sinh viên trong phòng thi
    public async Task GetStudentsInRoom(int examRoomId)
    {
        var students = _userConnections.Values
            .Where(u => u.ExamRoomId == examRoomId && u.UserType == "student")
            .Select(u => new
            {
                userId = u.UserId,
                studentCode = u.StudentCode,
                connectionId = u.ConnectionId
            })
            .ToList();

        await Clients.Caller.SendAsync("StudentsInRoom", students);
    }

    // Override OnDisconnectedAsync để xử lý khi user disconnect
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Xóa thông tin kết nối khi user disconnect
        if (_userConnections.TryRemove(Context.ConnectionId, out var userInfo))
        {
            if (userInfo.ExamRoomId.HasValue)
            {
                // Thông báo cho các user khác trong phòng biết có user disconnect
                await Clients.Group($"room_{userInfo.ExamRoomId}").SendAsync("UserDisconnected", new
                {
                    userId = userInfo.UserId,
                    userType = userInfo.UserType,
                    studentCode = userInfo.StudentCode,
                    connectionId = Context.ConnectionId
                });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Phương thức để kiểm tra trạng thái kết nối
    public async Task CheckConnectionStatus()
    {
        var userInfo = _userConnections.GetValueOrDefault(Context.ConnectionId);
        await Clients.Caller.SendAsync("ConnectionStatus", new
        {
            isConnected = userInfo != null,
            userInfo = userInfo
        });
    }
}