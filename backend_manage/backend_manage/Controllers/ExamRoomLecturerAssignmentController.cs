using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.Interfaces;
using backend_manage.shared.DTOs;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamRoomLecturerAssignmentController : ControllerBase
    {
        private readonly IExamRoomLecturerAssignmentService _service;
        public ExamRoomLecturerAssignmentController(IExamRoomLecturerAssignmentService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> Create([FromBody] ExamRoomLecturerAssignmentCreateDto dto)
        {
            try
            {
                var result = await _service.AddAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.ExamRoomLecturerAssignmentId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi tạo assignment." });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] ExamRoomLecturerAssignmentCreateDto dto)
        {
            try
            {
                var result = await _service.UpdateAsync(id, dto);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi cập nhật assignment." });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _service.DeleteAsync(id);
                if (!success) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi xóa assignment." });
            }
        }

        [HttpGet("my-assignments/{lecturerId}")]
        public async Task<IActionResult> GetRoomsByLecturer(int lecturerId)
        {
            var result = await _service.GetByLecturerIdAsync(lecturerId);
            return Ok(result);
        }

        // api/ExamRoomLecturerAssignment/my-assignments
        [HttpGet("my-assignments")]
        [Authorize(Policy = "LecturerOnly")]
        public async Task<IActionResult> GetMyAssignments()
        {
            try
            {
                var lecturerCode = User.FindFirst("lecturerCode")?.Value;
                if (string.IsNullOrEmpty(lecturerCode))
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin giảng viên trong token" });
                }

                var result = await _service.GetByLecturerCodeAsync(lecturerCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi lấy danh sách phân công" });
            }
        }
    }
}