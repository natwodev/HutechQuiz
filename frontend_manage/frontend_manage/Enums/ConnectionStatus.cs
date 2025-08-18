namespace frontend_manage.Enums
{
    /// <summary>
    /// Enum định nghĩa các trạng thái thi của sinh viên
    /// </summary>
    public enum ExamStatus
    {
        /// <summary>
        /// Chưa thi
        /// </summary>
        NotStarted = 0,
        
        /// <summary>
        /// Đang thi
        /// </summary>
        InProgress = 1,
        
        /// <summary>
        /// Bỏ thi
        /// </summary>
        Dropped = 2,
        
        /// <summary>
        /// Đã hoàn thành
        /// </summary>
        Completed = 3
    }
}
