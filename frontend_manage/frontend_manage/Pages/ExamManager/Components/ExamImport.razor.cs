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

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        protected override async Task OnInitializedAsync()
        {
            // Không cần load mock nữa
        }

        private async Task OnInputFileChange(InputFileChangeEventArgs e)
        {
            var file = e.File;
            selectedFileName = file.Name;
            selectedFileStream = file.OpenReadStream(10 * 1024 * 1024); // 10MB limit
            
            // Tự động tạo mã đề gốc từ tên file (bỏ đuôi .epz)
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
            if (selectedFileStream == null || string.IsNullOrWhiteSpace(originalExamPaperCore)) return;
            isUploading = true;
            importMessage = null;
            try
            {
                var result = await ExamManagerService.ImportOriginalExamXmlAsync(selectedFileStream, selectedFileName ?? "exam.epz", originalExamPaperCore);
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


