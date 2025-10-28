using Microsoft.AspNetCore.Components;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class StudentInfo : ComponentBase
    {
        [Parameter] public string StudentName { get; set; } = string.Empty;
        [Parameter] public string StudentCode { get; set; } = string.Empty;

        protected string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpperInvariant();
        }
    }
}


