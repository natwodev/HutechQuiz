using frontend_manage.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace frontend_manage.Services;


public class NotificationService
{
    private HubConnection? _hubConnection;

    public event Action<string, DateTime>? OnExamReminderReceived;
    public event Action<StudentListResponse>? OnRoomStatusUpdated;
    public event Action<object>? OnConnectedReceived;
    public event Action<object>? OnUserDisconnected;
    public event Action<object>? OnExamScoreReceived;
    public event Action<bool>? OnConnectionStateChanged;

    private readonly HashSet<int> _joinedGroups = new(); // để rejoin khi reconnect

    public async Task StartAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://0.0.0.0:5163/notificationHub")
            .WithAutomaticReconnect()
            .Build();

        // Lắng nghe sự kiện từ backend
        _hubConnection.On<string, DateTime>("ReceiveMessage", (message, examTime) =>
        {
            OnExamReminderReceived?.Invoke(message, examTime);
        });

        _hubConnection.On<StudentListResponse>("RoomStatusUpdated", (data) =>
        {
            OnRoomStatusUpdated?.Invoke(data);
        });
        
        _hubConnection.On<object>("Connected", (data) =>
        {
            OnConnectedReceived?.Invoke(data);
        });

        _hubConnection.On<object>("UserDisconnected", (data) =>
        {
            OnUserDisconnected?.Invoke(data);
        });

        
        _hubConnection.On<object>("ReceiveExamScore", (data) =>
        {
            OnExamScoreReceived?.Invoke(data);
        });
        
        
        // Theo dõi trạng thái kết nối
        _hubConnection.Closed += async (error) =>
        {
            OnConnectionStateChanged?.Invoke(false);
        };

        _hubConnection.Reconnecting += error =>
        {
            OnConnectionStateChanged?.Invoke(false);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += async connectionId =>
        {
            OnConnectionStateChanged?.Invoke(true);

            // join lại tất cả group cũ
            foreach (var groupId in _joinedGroups)
            {
                await _hubConnection.InvokeAsync("JoinLecturerView", groupId);
            }
        };

        try
        {
            await _hubConnection.StartAsync();
            OnConnectionStateChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            OnConnectionStateChanged?.Invoke(false);
            throw;
        }
    }

    // Gọi hàm JoinLecturerView bên backend - chỉ cần ExamSessionSubjectId
    public async Task JoinLecturerView(int examSessionSubjectId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinLecturerView", examSessionSubjectId);
            _joinedGroups.Add(examSessionSubjectId);
        }
        else
        {
            throw new InvalidOperationException("SignalR connection is not established");
        }
    }

    // Gọi hàm LeaveLecturerView bên backend
    public async Task LeaveLecturerView(int examSessionSubjectId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveLecturerView", examSessionSubjectId);
            _joinedGroups.Remove(examSessionSubjectId);
        }
    }

    // Tham gia group riêng của sinh viên để nhận điểm
    public async Task JoinStudentGroup(string studentCode)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinStudentGroup", studentCode);
        }
        else
        {
            throw new InvalidOperationException("SignalR connection is not established");
        }
    }

    // Rời group riêng của sinh viên
    public async Task LeaveStudentGroup(string studentCode)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveStudentGroup", studentCode);
        }
    }

    // Gửi message test
    public async Task SendMessage(string message, DateTime examTime)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendMessage", message, examTime);
        }
    }

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }
}
