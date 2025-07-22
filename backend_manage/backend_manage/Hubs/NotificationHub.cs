using Microsoft.AspNetCore.SignalR;

namespace backend_manage.Hubs;

public class NotificationHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task JoinRoom(int examRoomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{examRoomId}");
    }

    public async Task LeaveRoom(int examRoomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{examRoomId}");
    }

    public async Task SendRoomStatusUpdate(int examRoomId, object data)
    {
        await Clients.Group($"room_{examRoomId}").SendAsync("RoomStatusUpdated", data);
    }
}