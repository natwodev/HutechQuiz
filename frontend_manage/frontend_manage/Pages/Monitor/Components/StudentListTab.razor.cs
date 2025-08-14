using frontend_manage.Pages.Monitor.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace frontend_manage.Pages.Monitor.Components
{
    public partial class StudentListTab : ComponentBase
    {
        [Inject]
        private ISnackbar Snackbar { get; set; }

        private string searchText = "";
        private List<StudentDto> Students = new List<StudentDto>();
        private int currentPage = 1;
        private int pageSize = 10;
        private bool isLoading = true;

        private int TotalStudents => Students.Count;
        private int LoggedInStudents => Students.Count(s => s.LoginStatus == LoginStatus.LoggedIn);
        private int TakingExamStudents => Students.Count(s => s.ExamStatus == ExamStatus.TakingExam);
        private int SubmittedStudents => Students.Count(s => s.ExamStatus == ExamStatus.Submitted);

        private List<StudentDto> FilteredStudents
        {
            get
            {
                return string.IsNullOrWhiteSpace(searchText)
                    ? Students
                    : Students.Where(s =>
                        s.StudentCode.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        s.FullName.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        protected override async Task OnInitializedAsync()
        {
            // Chargement immédiat des 10 premiers étudiants
            await LoadInitialDataAsync();
        }
        
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Chargement différé des données restantes
                await LoadRemainingStudentsAsync();
            }
        }

        private async Task LoadInitialDataAsync()
        {
            // Chargement rapide des 10 premiers étudiants seulement
            Students = new List<StudentDto>
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

            isLoading = false;
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadRemainingStudentsAsync()
        {
            await Task.Delay(100); // Petit délai pour permettre au UI de se rendre
            
            var additionalStudents = new List<StudentDto>();
            // Chargement asynchrone des étudiants supplémentaires
            for (int i = 1; i <= 45; i++)
            {
                additionalStudents.Add(new StudentDto
                {
                    Id = i + 10,
                    Index = i + 10,
                    StudentCode = (i).ToString(),
                    FullName = $"SINH VIÊN THỬ NGHIỆM {i}",
                    ExamStatus = (i % 3 == 0) ? ExamStatus.Submitted :
                                (i % 3 == 1) ? ExamStatus.TakingExam : ExamStatus.NotStarted,
                    LoginStatus = (i % 5 != 0) ? LoginStatus.LoggedIn : LoginStatus.NotLoggedIn,
                    ExtraTime = (i % 7 == 0) ? 5 : 0,
                    Score = (i % 3 == 0) ? (double)(6.5 + (i % 4)) : null
                });
            }

            Students.AddRange(additionalStudents);
            await InvokeAsync(StateHasChanged);
        }

        private void LoadSampleData()
        {
            // Garder cette méthode pour la compatibilité avec RefreshData
            Students.Clear();
            isLoading = true;
            
            // Charger les premiers étudiants
            Students.AddRange(new List<StudentDto>
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
            });
            
            isLoading = false;

            // Ajout différé des étudiants supplémentaires
            InvokeAsync(async () => {
                await Task.Delay(200);
                for (int i = 1; i <= 45; i++)
                {
                    Students.Add(new StudentDto
                    {
                        Id = i + 10,
                        Index = i + 10,
                        StudentCode = (i).ToString(),
                        FullName = $"SINH VIÊN THỬ NGHIỆM {i}",
                        ExamStatus = (i % 3 == 0) ? ExamStatus.Submitted :
                                    (i % 3 == 1) ? ExamStatus.TakingExam : ExamStatus.NotStarted,
                        LoginStatus = (i % 5 != 0) ? LoginStatus.LoggedIn : LoginStatus.NotLoggedIn,
                        ExtraTime = (i % 7 == 0) ? 5 : 0,
                        Score = (i % 3 == 0) ? (double)(6.5 + (i % 4)) : null
                    });
                }
                StateHasChanged();
            });
        }

        private void PerformSearch()
        {
            currentPage = 1; // Reset to first page when searching
            StateHasChanged();
        }

        private void RefreshData()
        {
            LoadSampleData();
            Snackbar.Add("Dữ liệu đã được làm mới", Severity.Success);
        }

        private void ViewStudent(int id)
        {
            Snackbar.Add($"Xem chi tiết sinh viên ID: {id}", Severity.Info);
        }

        private void MessageStudent(int id)
        {
            Snackbar.Add($"Gửi thông báo cho sinh viên ID: {id}", Severity.Info);
        }

        private void ManageStudent(int id)
        {
            Snackbar.Add($"Quản lý sinh viên ID: {id}", Severity.Info);
        }
    }
}
