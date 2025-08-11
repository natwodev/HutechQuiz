using Microsoft.AspNetCore.SignalR;

namespace backend_manage.core.Hubs
{
    public class NotificationHub : Hub
    {
        private static string LecturerGroupName(int examRoomId, int examSessionSubjectId)
            => $"lecturer_room_{examRoomId}_{examSessionSubjectId}";

        public async Task SendMessage(string message, DateTime examTime)
        {
            await Clients.All.SendAsync("ReceiveMessage", message, examTime);
        }

        public async Task JoinLecturerView(int examRoomId, int examSessionSubjectId)
        {
            var group = LecturerGroupName(examRoomId, examSessionSubjectId);
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            Console.WriteLine($"[SignalR] Lecturer {Context.ConnectionId} joined {group}");
        }

        public async Task LeaveLecturerView(int examRoomId, int examSessionSubjectId)
        {
            var group = LecturerGroupName(examRoomId, examSessionSubjectId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            Console.WriteLine($"[SignalR] Lecturer {Context.ConnectionId} left {group}");
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", new
            {
                connectionId = Context.ConnectionId,
                message = "Kết nối tới NotificationHub thành công."
            });
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Clients.All.SendAsync("UserDisconnected", new
            {
                connectionId = Context.ConnectionId
            });
            await base.OnDisconnectedAsync(exception);
        }
    }
}