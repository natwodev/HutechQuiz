using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using frontend_manage.Services.ExamManager;
using MudBlazor;
using System.Collections.Generic;
using System.Linq;
using frontend_manage.DTOs;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace frontend_manage.Pages.ExamManager.Components
{
    public partial class ExamPermutation : ComponentBase
    {
        [Inject]
        private ExamManagerService ExamManagerService { get; set; }

        private string originalExamPaperCore = string.Empty;
        private int count = 1;
        private bool isCreating;
        private string? message;
        [Inject]
        private IDialogService DialogService { get; set; }
        [Inject]
        private NavigationManager NavigationManager { get; set; }
        [Inject]
        private IJSRuntime JSRuntime { get; set; }
        private List<ShuffledExamPaperDto> shuffledItems = new();
        private string? selectedShuffledCore;

        private async Task CreateShuffledAsync()
        {
            isCreating = true;
            message = null;
            try
            {
                var result = await ExamManagerService.CreateShuffledPapersAsync(originalExamPaperCore, count);
                message = result?.Message;
            }
            catch (Exception ex)
            {
                message = $"Lỗi: {ex.Message}";
            }
            finally
            {
                isCreating = false;
            }
        }

        private async Task PreviewShuffledMock()
        {
            if (shuffledItems == null || shuffledItems.Count == 0)
            {
                shuffledItems = await ExamManagerService.GetShuffledExamsMockAsync();
            }
            var item = !string.IsNullOrWhiteSpace(selectedShuffledCore)
                ? shuffledItems.FirstOrDefault(i => i.ShuffledExamPaperCore == selectedShuffledCore)
                : shuffledItems.FirstOrDefault();
            var subjectName = item?.SubjectName ?? "Lập trình C#";
            var subjectCode = item?.SubjectCode ?? "CS101";
            var core = item?.ShuffledExamPaperCore ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(core))
            {
                var uri = $"/exammanager/preview/shuffled?core={Uri.EscapeDataString(core)}&subjectName={Uri.EscapeDataString(subjectName)}&subjectCode={Uri.EscapeDataString(subjectCode)}";
                await JSRuntime.InvokeVoidAsync("open", uri, "_blank");
            }
        }

        protected override async Task OnInitializedAsync()
        {
            shuffledItems = await ExamManagerService.GetShuffledExamsMockAsync();
        }

        private void OnSelectChanged(ChangeEventArgs e)
        {
            selectedShuffledCore = e.Value?.ToString();
        }
    }
}


