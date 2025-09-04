using Microsoft.AspNetCore.SignalR;

namespace backend_manage.core.Hubs
{
    public class NotificationHub : Hub
    {
        private static string LecturerGroupName(int examSessionSubjectId)
            => $"lecturer_subject_{examSessionSubjectId}";

        public async Task SendMessage(string message, DateTime examTime)
        {
            await Clients.All.SendAsync("ReceiveMessage", message, examTime);
        }

            public async Task JoinLecturerView(int examSessionSubjectId)
    {
        var group = LecturerGroupName(examSessionSubjectId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

        public async Task LeaveLecturerView(int examSessionSubjectId)
        {
            var group = LecturerGroupName(examSessionSubjectId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
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