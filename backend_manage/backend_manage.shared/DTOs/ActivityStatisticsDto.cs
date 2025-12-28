namespace backend_manage.shared.DTOs;

public class ActivityStatisticsDto
{
    public int TotalActivities { get; set; }
    public Dictionary<string, int> ActivitiesByType { get; set; } = new();
    public Dictionary<string, int> ActivitiesByStudent { get; set; } = new();
}

