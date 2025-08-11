using Microsoft.AspNetCore.SignalR.Client;

namespace frontend_manage.Services;

public class NotificationService
{
    private HubConnection _hubConnection;

    public event Action<string, DateTime> OnExamReminderReceived;

    public async Task StartAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://0.0.0.0:5163/notificationHub")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string, DateTime>("ReceiveMessage", (message, examTime) =>
        {
            OnExamReminderReceived?.Invoke(message, examTime);
        });

        await _hubConnection.StartAsync();
    }

    
    // Gọi hàm JoinLecturerView bên backend
    public async Task JoinLecturerView(int examRoomId, int examSessionSubjectId)
    {
        await _hubConnection.InvokeAsync("JoinLecturerView", examRoomId, examSessionSubjectId);
    }
    
    // Gọi hàm LeaveLecturerView bên backend
    public async Task LeaveLecturerView(int examRoomId, int examSessionSubjectId)
    {
        await _hubConnection.InvokeAsync("LeaveLecturerView", examRoomId, examSessionSubjectId);
    }
    

}

