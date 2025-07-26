using Microsoft.AspNetCore.SignalR.Client;

namespace frontend_manage.Services;

public class NotificationService
{
    private HubConnection _hubConnection;

    public event Action<string, DateTime> OnExamReminderReceived;
    public event Action 
        RoomStatusUpdated;

    public async Task StartAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://0.0.0.0:5163/notificationHub")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string, DateTime>("ExamReminder", (message, examTime) =>
        {
            OnExamReminderReceived?.Invoke(message, examTime);
        });

        _hubConnection.On<object>("RoomStatusUpdated", (data) =>
        {
            Console.WriteLine("[SignalR] Đã nhận RoomStatusUpdated từ SignalR (NotificationService)");
            RoomStatusUpdated?.Invoke();
        });

        await _hubConnection.StartAsync();
        Console.WriteLine("[SignalR] Đã kết nối tới notificationHub!");
    }
}

