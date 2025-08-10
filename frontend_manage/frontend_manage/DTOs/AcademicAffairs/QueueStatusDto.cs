namespace frontend_manage.DTOs.AcademicAffairs
{
    public class QueueStatusDto
    {
        public string QueueName { get; set; }
        public int MessageCount { get; set; }
        public bool IsConnected { get; set; }
        public string Status { get; set; }
    }
}
