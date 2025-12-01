using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using frontend_manage.Services.ExamManager;
using MudBlazor;
using System.Collections.Generic;
using System.Linq;
using frontend_manage.DTOs;
using Microsoft.JSInterop;
using frontend_manage.Services;

namespace frontend_manage.Pages.ExamManager.Components
{
    public partial class ExamPermutation : ComponentBase
    {
        [Inject]
        private ExamManagerService ExamManagerService { get; set; }

        [Inject]
        private AuthService AuthService { get; set; }

        private string originalExamPaperCore = string.Empty;
        private int count = 1;
        private bool isCreating;
        private bool isLoadingOriginalExams;
        private string? message;
        private List<OriginalExamPaperSelectItem> originalExamPaperOptions = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadOriginalExamPapers();
        }

        private async Task LoadOriginalExamPapers()
        {
            isLoadingOriginalExams = true;
            try
            {
                var role = await AuthService.GetUserRoleFromToken();
                var canView = role == "Admin" || role == "ExamManager";
                
                if (canView)
                {
                    var examPapers = await ExamManagerService.GetAllOriginalExamPapersAsync();
                    originalExamPaperOptions = examPapers.Select(x => new OriginalExamPaperSelectItem
                    {
                        Value = x.OriginalExamPaperCore,
                        Text = $"{x.OriginalExamPaperCore} - {x.Title}"
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                message = $"Lỗi khi tải danh sách đề gốc: {ex.Message}";
            }
            finally
            {
                isLoadingOriginalExams = false;
                StateHasChanged();
            }
        }


        private async Task CreateShuffledAsync()
        {
            if (string.IsNullOrWhiteSpace(originalExamPaperCore) || count < 1)
                return;

            isCreating = true;
            message = null;
            try
            {
                var result = await ExamManagerService.CreateShuffledPapersAsync(originalExamPaperCore, count);
                message = result?.Message ?? "Tạo đề hoán vị thành công.";
                
                // Reload danh sách đề gốc để cập nhật số lượng đề hoán vị
                await LoadOriginalExamPapers();
            }
            catch (Exception ex)
            {
                message = $"Lỗi: {ex.Message}";
            }
            finally
            {
                isCreating = false;
                StateHasChanged();
            }
        }
    }

    public class OriginalExamPaperSelectItem
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}


