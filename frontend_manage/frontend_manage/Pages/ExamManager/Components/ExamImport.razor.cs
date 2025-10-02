using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using frontend_manage.DTOs;
using frontend_manage.Services.ExamManager;
using MudBlazor;

namespace frontend_manage.Pages.ExamManager.Components
{
    public partial class ExamImport : ComponentBase
    {
        [Inject]
        private ExamManagerService ExamManagerService { get; set; }

        [Inject]
        private IDialogService DialogService { get; set; }

        private Stream? selectedFileStream;
        private string? selectedFileName;
        private bool isUploading;
        private string? importMessage;
        private string originalExamPaperCore = string.Empty;

        private OriginalExamPaperDto? originalExam;
        private bool isLoadingDetails;

        private int _pageIndex = 1;
        private int _pageSize = 10;

        private IEnumerable<OriginalExamPaperDetailDto> PagedItems =>
            originalExam?.Details
                .OrderBy(d => d.Order)
                .Skip((_pageIndex - 1) * _pageSize)
                .Take(_pageSize) ?? Enumerable.Empty<OriginalExamPaperDetailDto>();

        private int TotalPages => originalExam == null || originalExam.Details.Count == 0
            ? 1
            : (int)Math.Ceiling(originalExam.Details.Count / (double)_pageSize);

        protected override async Task OnInitializedAsync()
        {
            // Auto-load mock so the dashboard shows data immediately
            await LoadMock();
        }

        private async Task OnInputFileChange(InputFileChangeEventArgs e)
        {
            var file = e.File;
            selectedFileName = file.Name;
            selectedFileStream = file.OpenReadStream(10 * 1024 * 1024); // 10MB limit
        }

        private async Task UploadAsync()
        {
            if (selectedFileStream == null || string.IsNullOrWhiteSpace(originalExamPaperCore)) return;
            isUploading = true;
            importMessage = null;
            try
            {
                var result = await ExamManagerService.ImportOriginalExamXmlAsync(selectedFileStream, selectedFileName ?? "exam.xml", originalExamPaperCore);
                importMessage = result?.Message;
                await LoadDetailsAsync(originalExamPaperCore);
            }
            catch (Exception ex)
            {
                importMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                isUploading = false;
                StateHasChanged();
            }
        }

        private async Task LoadDetailsAsync(string core)
        {
            isLoadingDetails = true;
            try
            {
                originalExam = await ExamManagerService.GetOriginalExamWithDetailsAsync(core);
                _pageIndex = 1;
            }
            finally
            {
                isLoadingDetails = false;
            }
        }

        private void PrevPage()
        {
            if (_pageIndex > 1) _pageIndex--;
        }

        private void NextPage()
        {
            if (_pageIndex < TotalPages) _pageIndex++;
        }

        private async Task LoadMock()
        {
            isLoadingDetails = true;
            try
            {
                // Prefer generated mock to ensure 100+ items and avoid caching issues
                originalExam = await ExamManagerService.GenerateOriginalExamMockAsync(100);
                importMessage = "Đã tải dữ liệu mock.";
                _pageIndex = 1;
            }
            finally
            {
                isLoadingDetails = false;
            }
        }

        private async Task Preview(OriginalExamPaperDetailDto item)
        {
            var parameters = new DialogParameters
            {
                ["Question"] = item
            };
            var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            await DialogService.ShowAsync<PreviewQuestionDialog>("Xem trước câu hỏi", parameters, options);
        }
    }
}


