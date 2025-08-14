using Microsoft.AspNetCore.Components;
using System;
using System.Threading;

namespace frontend_manage.Pages.Monitor
{
    public partial class MonitorPage : ComponentBase, IDisposable
    {
        private int activeTab = 0;
        private int previousTab = 0;
        private string currentDate = DateTime.Now.ToString("dd/MM/yyyy");
        private string currentTime = DateTime.Now.ToString("HH:mm:ss");
        private Timer timer;

        protected override void OnInitialized()
        {
            timer = new Timer(_ =>
            {
                currentTime = DateTime.Now.ToString("HH:mm:ss");
                InvokeAsync(StateHasChanged);
            }, null, 0, 1000);
        }

        private void ActivateTab(int tabIndex)
        {
            previousTab = activeTab;
            activeTab = tabIndex;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}
