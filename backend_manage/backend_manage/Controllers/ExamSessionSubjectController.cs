using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExamSessionSubjectController : ControllerBase
    {
        private readonly IExamSessionSubjectService _examSessionSubjectService;

        public ExamSessionSubjectController(IExamSessionSubjectService examSessionSubjectService)
        {
            _examSessionSubjectService = examSessionSubjectService;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExamSessionSubjectDto>>> GetAll()
        {
            try
            {
                var result = await _examSessionSubjectService.GetAllAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<ExamSessionSubjectDto>> GetById(int id)
        {
            try
            {
                var result = await _examSessionSubjectService.GetByIdAsync(id);
                if (result == null)
                    return NotFound($"Không tìm thấy ExamSessionSubject với ID: {id}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ExamSessionSubjectDto>> Create(ExamSessionSubjectCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _examSessionSubjectService.AddAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.ExamSessionSubjectId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }
        
        [HttpPut("{id}")]
        public async Task<ActionResult<ExamSessionSubjectDto>> Update(int id, ExamSessionSubjectUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _examSessionSubjectService.UpdateAsync(id, dto);
                if (result == null)
                    return NotFound($"Không tìm thấy ExamSessionSubject với ID: {id}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }
        
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var result = await _examSessionSubjectService.DeleteAsync(id);
                if (!result)
                    return NotFound($"Không tìm thấy ExamSessionSubject với ID: {id}");
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }
        
        [HttpGet("room/{examRoomId}")]
        public async Task<ActionResult<IEnumerable<ExamSessionSubjectDto>>> GetByExamRoomId(int examRoomId)
        {
            try
            {
                var result = await _examSessionSubjectService.GetByExamRoomIdAsync(examRoomId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }
        
        [HttpGet("lecturer/{lecturerId}")]
        public async Task<ActionResult<IEnumerable<ExamSessionSubjectDto>>> GetByLecturerId(int lecturerId)
        {
            try
            {
                var result = await _examSessionSubjectService.GetByLecturerIdAsync(lecturerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }

        [HttpGet("{examSessionSubjectId}/is-open")]
        public async Task<ActionResult<bool>> IsOpen(int examSessionSubjectId)
        {
            try
            {
                var isOpen = await _examSessionSubjectService.IsOpenAsync(examSessionSubjectId);
                return Ok(isOpen);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }
        
        [HttpPost("assign-lecturer")]
        public async Task<ActionResult> AssignLecturer(AssignLecturerDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                await _examSessionSubjectService.AssignLecturerAsync(dto);
                return Ok(new { message = "Phân công giảng viên thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }
        
        [HttpPost("unassign-lecturer")]
        public async Task<ActionResult> UnassignLecturer(UnassignLecturerDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                await _examSessionSubjectService.UnassignLecturerAsync(dto);
                return Ok(new { message = "Hủy phân công giảng viên thành công" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }
        
        [HttpPut("{examSessionSubjectId}/exam-room")]
        public async Task<ActionResult> UpdateExamRoom(int examSessionSubjectId, [FromBody] int? examRoomId)
        {
            try
            {
                var result = await _examSessionSubjectService.UpdateExamRoomIdAsync(examSessionSubjectId, examRoomId);
                if (!result)
                    return NotFound($"Không tìm thấy ExamSessionSubject với ID: {examSessionSubjectId}");

                return Ok(new { message = "Cập nhật phòng thi thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }
        
        [HttpPut("{examSessionSubjectId}/original-exam-paper")]
        [Authorize(Policy = "StaffOnly")]
        public async Task<ActionResult> UpdateOriginalExamPaper(int examSessionSubjectId, [FromBody] int originalExamPaperId)
        {
            try
            {
                var result = await _examSessionSubjectService.UpdateOriginalExamPaperIdAsync(examSessionSubjectId, originalExamPaperId);
                if (!result)
                    return NotFound($"Không tìm thấy ExamSessionSubject với ID: {examSessionSubjectId}");

                return Ok(new { message = "Cập nhật đề thi gốc thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }

        [HttpPost("is-active")]
        public async Task<ActionResult> UpdateIsActive([FromBody] ActiveExamSessionSubject request)
        {
            try
            {
                await _examSessionSubjectService.UpdateIsActiveAsync(request.examSessionSubjectId, request.isActive);

                return Ok(new { message = "Cập nhật trạng thái hoạt động thành công" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }

        public class ActiveExamSessionSubject
        {
            public int examSessionSubjectId { get; set; }
            public bool isActive { get; set; }
        }
        
        [HttpGet("{examSessionSubjectId}/with-students")]
        public async Task<ActionResult<object>> GetWithStudents(int examSessionSubjectId)
        {
            try
            {
                var result = await _examSessionSubjectService.GetExamSessionSubjectWithStudentsAsync(examSessionSubjectId);
                return Ok(new
                {
                    Subject = result.SubjectExamRoomStatus,
                    Students = result.Students
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }


        [HttpGet("lecturer/subject-exam-room-status")]
        public async Task<ActionResult<IEnumerable<SubjectExamRoomStatusDto>>> GetSubjectExamRoomStatusByLecturerId()
        {
            try
            {
                var lecturerId = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(lecturerId))
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin giảng viên trong token" });
                }

                if (!int.TryParse(lecturerId, out int id))
                {
                    return BadRequest(new { message = "ID giảng viên không hợp lệ" });
                }

                var result = await _examSessionSubjectService.GetSubjectExamRoomStatusByLecturerIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }

        [HttpPut("{examSessionSubjectId}/students/toggle-is-login")]
        public async Task<ActionResult> ToggleIsLoginForAllStudents(int examSessionSubjectId, [FromBody] ToggleIsLoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var (success, message, updatedCount) = await _examSessionSubjectService.ToggleIsLoginForAllStudentsAsync(
                    examSessionSubjectId, 
                    request.IsLogin);

                if (!success)
                    return BadRequest(new { message, updatedCount });

                return Ok(new { message, updatedCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi nội bộ: {ex.Message}");
            }
        }

        public class ToggleIsLoginRequest
        {
            public bool IsLogin { get; set; }
        }
    }
} 