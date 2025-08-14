using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace frontend_manage.Services
{
    public class ExamMonitorService
    {
        private readonly HttpClient _httpClient;
        private readonly NavigationManager _navigationManager;
        private readonly ISnackbar _snackbar;
        private HubConnection _hubConnection;

        // Events pour les mises à jour en temps réel
        public event Action<StudentDto> OnStudentStatusUpdated;
        public event Action<List<StudentDto>> OnStudentsListUpdated;
        public event Action<string> OnExamStatusUpdated;

        public ExamMonitorService(HttpClient httpClient, NavigationManager navigationManager, ISnackbar snackbar)
        {
            _httpClient = httpClient;
            _navigationManager = navigationManager;
            _snackbar = snackbar;
        }

        public async Task InitializeSignalRConnection(string examRoomCode)
        {
            if (_hubConnection == null)
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(_navigationManager.ToAbsoluteUri($"/exammonitorhub?examRoom={examRoomCode}"))
                    .WithAutomaticReconnect()
                    .Build();

                // Configurer les gestionnaires d'événements pour les mises à jour en temps réel
                _hubConnection.On<StudentDto>("StudentStatusUpdated", (student) =>
                {
                    OnStudentStatusUpdated?.Invoke(student);
                });

                _hubConnection.On<List<StudentDto>>("StudentsListUpdated", (students) =>
                {
                    OnStudentsListUpdated?.Invoke(students);
                });

                _hubConnection.On<string>("ExamStatusUpdated", (status) =>
                {
                    OnExamStatusUpdated?.Invoke(status);
                });

                // Gérer la reconnexion
                _hubConnection.Closed += async (error) =>
                {
                    _snackbar.Add("Mất kết nối với máy chủ. Đang thử kết nối lại...", Severity.Warning);
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await _hubConnection.StartAsync();
                };

                try
                {
                    await _hubConnection.StartAsync();
                    _snackbar.Add("Đã kết nối với máy chủ để nhận thông tin cập nhật", Severity.Success);
                }
                catch (Exception ex)
                {
                    _snackbar.Add($"Không thể kết nối với máy chủ: {ex.Message}", Severity.Error);
                }
            }
        }

        public async Task<List<StudentDto>> GetStudentsInExamRoomAsync(string examRoomCode, string subjectCode)
        {
            try
            {
                // Dans une application réelle, appelez votre API
                // var response = await _httpClient.GetFromJsonAsync<List<StudentDto>>($"api/monitor/students?examRoom={examRoomCode}&subject={subjectCode}");
                // return response;
                
                // Pour la démo, on retourne des données fictives
                return GetSampleData();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Lỗi khi tải danh sách sinh viên: {ex.Message}", Severity.Error);
                return new List<StudentDto>();
            }
        }

        public async Task SendNotificationToAllStudentsAsync(string examRoomCode, string message)
        {
            try
            {
                // Dans une application réelle, appelez votre API
                // await _httpClient.PostAsJsonAsync($"api/monitor/notify/all?examRoom={examRoomCode}", 
                //     new { message = message });
                
                _snackbar.Add("Đã gửi thông báo cho tất cả sinh viên trong phòng thi", Severity.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Lỗi khi gửi thông báo: {ex.Message}", Severity.Error);
            }
        }

        public async Task SendNotificationToStudentAsync(int studentId, string message)
        {
            try
            {
                // Dans une application réelle, appelez votre API
                // await _httpClient.PostAsJsonAsync($"api/monitor/notify/student/{studentId}", 
                //     new { message = message });
                
                _snackbar.Add($"Đã gửi thông báo cho sinh viên ID: {studentId}", Severity.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Lỗi khi gửi thông báo: {ex.Message}", Severity.Error);
            }
        }

        public async Task AddExtraTimeAsync(int studentId, int minutes)
        {
            try
            {
                // Dans une application réelle, appelez votre API
                // await _httpClient.PostAsJsonAsync($"api/monitor/extratime/{studentId}", 
                //     new { minutes = minutes });
                
                _snackbar.Add($"Đã thêm {minutes} phút cho sinh viên ID: {studentId}", Severity.Success);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Lỗi khi thêm thời gian: {ex.Message}", Severity.Error);
            }
        }

        public async Task LockExamAsync(string examRoomCode)
        {
            try
            {
                // Dans une application réelle, appelez votre API
                // await _httpClient.PutAsync($"api/monitor/lock?examRoom={examRoomCode}", null);
                
                _snackbar.Add("Đã khóa ca thi", Severity.Warning);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Lỗi khi khóa ca thi: {ex.Message}", Severity.Error);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        // Méthode pour générer des données d'exemple
        private List<StudentDto> GetSampleData()
        {
            var students = new List<StudentDto>
            {
                new StudentDto { Id = 1, Index = 11, StudentCode = "11", FullName = "LÊ VĂN HIỀN", ExamStatus = ExamStatus.Submitted, LoginStatus = LoginStatus.LoggedIn, ExtraTime = 0, Score = 8.0 },
                new StudentDto { Id = 2, Index = 12, StudentCode = "12", FullName = "NGUYỄN THỊ HƯƠNG", ExamStatus = ExamStatus.Submitted, LoginStatus = LoginStatus.LoggedIn, ExtraTime = 0, Score = 8.5 },
                new StudentDto { Id = 3, Index = 13, StudentCode = "13", FullName = "TRỊNH VĂN KHANG", ExamStatus = ExamStatus.TakingExam, LoginStatus = LoginStatus.LoggedIn, ExtraTime = 5, Score = null },
                new StudentDto { Id = 4, Index = 14, StudentCode = "14", FullName = "PHAN THỊ LINH", ExamStatus = ExamStatus.Submitted, LoginStatus = LoginStatus.LoggedIn, ExtraTime = 0, Score = 8.0 },
                new StudentDto { Id = 5, Index = 15, StudentCode = "15", FullName = "VŨ QUANG MINH", ExamStatus = ExamStatus.Submitted, LoginStatus = LoginStatus.LoggedIn, ExtraTime = 0, Score = 7.5 },
                new StudentDto { Id = 6, Index = 16, StudentCode = "16", FullName = "LÊ THỊ NGỌC", ExamStatus = ExamStatus.Submitted, LoginStatus = LoginStatus.LoggedIn, ExtraTime = 0, Score = 9.0 },
                new StudentDto { Id = 7, Index = 17, StudentCode = "17", FullName = "NGUYỄN TRUNG HIẾU", ExamStatus = ExamStatus.NotStarted, LoginStatus = LoginStatus.NotLoggedIn, ExtraTime = 0, Score = null },
                new StudentDto { Id = 8, Index = 18, StudentCode = "18", FullName = "HOÀNG MINH PHƯƠNG", ExamStatus = ExamStatus.Submitted, LoginStatus = LoginStatus.LoggedIn, ExtraTime = 0, Score = 9.5 },
                new StudentDto { Id = 9, Index = 19, StudentCode = "19", FullName = "PHẠM QUỐC TUẤN", ExamStatus = ExamStatus.TakingExam, LoginStatus = LoginStatus.LoggedIn, ExtraTime = 10, Score = null },
                new StudentDto { Id = 10, Index = 20, StudentCode = "20", FullName = "TRẦN THANH THỦY", ExamStatus = ExamStatus.Submitted, LoginStatus = LoginStatus.LoggedIn, ExtraTime = 0, Score = 8.5 }
            };
            
            // Ajouter plus d'étudiants pour tester la pagination
            for (int i = 11; i <= 45; i++)
            {
                students.Add(new StudentDto
                {
                    Id = i,
                    Index = i + 10,
                    StudentCode = (i + 10).ToString(),
                    FullName = $"SINH VIÊN THỬ NGHIỆM {i}",
                    ExamStatus = (i % 3 == 0) ? ExamStatus.Submitted : 
                                (i % 3 == 1) ? ExamStatus.TakingExam : ExamStatus.NotStarted,
                    LoginStatus = (i % 5 != 0) ? LoginStatus.LoggedIn : LoginStatus.NotLoggedIn,
                    ExtraTime = (i % 7 == 0) ? 5 : 0,
                    Score = (i % 3 == 0) ? (double)(6.5 + (i % 4)) : null
                });
            }
            
            return students;
        }
    }
}
