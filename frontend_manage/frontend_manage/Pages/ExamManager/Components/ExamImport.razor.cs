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
using Microsoft.JSInterop;
using frontend_manage.DTOs.AcademicAffairs;

namespace frontend_manage.Pages.ExamManager.Components
{
    public partial class ExamImport : ComponentBase
    {
        [Inject]
        private ExamManagerService ExamManagerService { get; set; }

        [Inject]
        private IDialogService DialogService { get; set; }

        private IBrowserFile? selectedFile;
        private string? selectedFileName;
        private bool isUploading;
        private string? importMessage;
        private string originalExamPaperCore = string.Empty;
        private string selectedImportType = "xml"; // "xml" hoặc "word"
        private int selectedSubjectId;
        private List<SubjectDto> subjects = new();

        private bool IsUploadDisabled =>
            isUploading ||
            selectedFile == null ||
            string.IsNullOrWhiteSpace(originalExamPaperCore) ||
            (selectedImportType == "word" && selectedSubjectId <= 0);

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                subjects = await ExamManagerService.GetAllSubjectsAsync();
            }
            catch (Exception ex)
            {
                importMessage = $"Lỗi khi tải danh sách môn học: {ex.Message}";
            }
        }

        private async Task OnInputFileChange(InputFileChangeEventArgs e)
        {
            selectedFile = e.File;
            selectedFileName = selectedFile.Name;
            
            // Tự động tạo mã đề gốc từ tên file (bỏ đuôi)
            if (string.IsNullOrWhiteSpace(originalExamPaperCore) && !string.IsNullOrEmpty(selectedFileName))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(selectedFileName);
                // Loại bỏ ký tự đặc biệt, chỉ giữ chữ, số, dấu gạch dưới và gạch ngang
                originalExamPaperCore = System.Text.RegularExpressions.Regex.Replace(
                    fileNameWithoutExtension, 
                    @"[^a-zA-Z0-9_-]", 
                    "_"
                );
                
                // Nếu vẫn rỗng, tạo mã tự động
                if (string.IsNullOrWhiteSpace(originalExamPaperCore))
                {
                    originalExamPaperCore = $"EXP_{DateTime.Now:yyyyMMddHHmmss}";
                }
            }
            
            StateHasChanged();
        }
        
        private async Task GenerateAutoCode()
        {
            try
            {
                originalExamPaperCore = await ExamManagerService.GenerateRandomOriginalExamPaperCoreAsync();
                importMessage = null; // Clear any previous error messages
            }
            catch (Exception ex)
            {
                importMessage = $"Lỗi khi tạo mã tự động: {ex.Message}";
            }
            finally
            {
                StateHasChanged();
            }
        }

        private async Task UploadAsync()
        {
            if (selectedFile == null || string.IsNullOrWhiteSpace(originalExamPaperCore)) return;
            if (selectedImportType == "word" && selectedSubjectId <= 0)
            {
                importMessage = "Vui lòng chọn môn học trước khi import từ Word.";
                return;
            }
            isUploading = true;
            importMessage = null;
            try
            {
                using var stream = selectedFile.OpenReadStream(20 * 1024 * 1024); // Tăng giới hạn lên 20MB
                ImportResultDto? result;
                if (selectedImportType == "xml")
                {
                    result = await ExamManagerService.ImportOriginalExamXmlAsync(
                        stream,
                        selectedFileName ?? "exam.epz",
                        originalExamPaperCore);
                }
                else
                {
                    result = await ExamManagerService.ImportOriginalExamWordAsync(
                        stream,
                        selectedFileName ?? "exam.docx",
                        originalExamPaperCore,
                        selectedSubjectId);
                }
                importMessage = result?.Message;
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

        private async Task OpenPreviewInNewTab()
        {
            if (string.IsNullOrWhiteSpace(originalExamPaperCore)) return;
            
            var uri = $"/exammanager/preview/original?core={Uri.EscapeDataString(originalExamPaperCore)}";
            await JSRuntime.InvokeVoidAsync("open", uri, "_blank");
        }
    }
}


