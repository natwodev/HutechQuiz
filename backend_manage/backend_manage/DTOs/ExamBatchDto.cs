using System;
using System.Collections.Generic;

namespace backend_manage.DTOs
{
    public class ExamBatchDto
    {
        public int ExamBatchId { get; set; }
        public string BatchName { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int SemesterId { get; set; }
        public string SemesterName { get; set; }
        public bool IsActive { get; set; }
        public List<ExamBatchDetailDto> ExamBatchDetails { get; set; }
    }

    public class ExamBatchCreateDto
    {
        public string BatchName { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int SemesterId { get; set; }
        public bool IsActive { get; set; }
        public List<ExamBatchDetailCreateDto> ExamBatchDetails { get; set; }
    }

    public class ExamBatchUpdateDto
    {
        public string BatchName { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int SemesterId { get; set; }
        public bool IsActive { get; set; }
        public List<ExamBatchDetailUpdateDto> ExamBatchDetails { get; set; }
    }
} 