using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace backend_manage.core.Services.Templates
{
    public static class LecturerExcelTemplate
    {
        public static byte[] Generate()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Mẫu Giảng Viên");

            int row = 1;

            // ===== TIÊU ĐỀ =====
            worksheet.Cells[row, 1].Value = "DANH SÁCH CÁN BỘ THAM GIA COI THI";
            worksheet.Cells[row, 1, row, 9].Merge = true;
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Size = 18;
            worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Row(row).Height = 30;
            row += 2;

            // ===== THÔNG TIN CHUNG =====
            worksheet.Cells[row, 1].Value = "Học kỳ:";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Value = "(VD: Học kỳ 3 hoặc Học kỳ phụ - HK3)";
            row++;
            worksheet.Cells[row, 1].Value = "Năm học:";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Value = "(VD: 2024 - 2025)";
            row++;
            worksheet.Cells[row, 1].Value = "Thời gian thi:";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Value = "Từ ngày ... đến ngày ...";
            row += 2;

            // ===== PHÒNG THI =====
            worksheet.Cells[row, 1].Value = "Ghi chú về ký hiệu phòng thi:";
            worksheet.Cells[row, 1, row, 9].Merge = true;
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            row++;
            worksheet.Cells[row, 1].Value = "A, B, C:";
            worksheet.Cells[row, 2].Value = "Sai Gon Campus (475A Điện Biên Phủ)";
            row++;
            worksheet.Cells[row, 1].Value = "E1:";
            worksheet.Cells[row, 2].Value = "Thu Duc Campus (Khu CNC Thủ Đức)";
            row += 2;

            // ===== THỜI GIAN CÓ MẶT =====
            worksheet.Cells[row, 1].Value = "THỜI GIAN CÓ MẶT TẠI PHÒNG HỘI ĐỒNG THI";
            worksheet.Cells[row, 1, row, 9].Merge = true;
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.LightSteelBlue);
            row++;
            worksheet.Cells[row, 1].Value = "- Buổi sáng:";
            worksheet.Cells[row, 2].Value = "06h50";
            row++;
            worksheet.Cells[row, 1].Value = "- Buổi chiều:";
            worksheet.Cells[row, 2].Value = "12h50";
            row++;
            worksheet.Cells[row, 1].Value = "- Buổi tối:";
            worksheet.Cells[row, 2].Value = "17h30";
            row++;
            worksheet.Cells[row, 1].Value = "(Ngày đầu tiên CBCT có mặt tại hội đồng thi lúc 06h45)";
            worksheet.Cells[row, 1, row, 9].Merge = true;
            worksheet.Cells[row, 1].Style.Font.Italic = true;
            row += 2;

            // ===== BẢNG DỮ LIỆU =====
            int headerRow = row; // Lưu hàng header để đặt ghi chú bên phải
            string[] headers = {
                "STT", "Mã giảng viên", "Họ", "Tên", "Giới tính",
                "Ngày sinh", "Email", "Số điện thoại", "Mã khoa"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[row, i + 1].Value = headers[i];
                worksheet.Cells[row, i + 1].Style.Font.Bold = true;
                worksheet.Cells[row, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(180, 210, 240));
                worksheet.Cells[row, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
            row++;

            // Thêm 1 dòng mẫu
            string[] sample = { "1", "GV001", "Nguyễn", "Văn A", "Nam", "01/01/1980", "nguyenvana@example.com", "0123456789", "CNTT" };
            for (int i = 0; i < sample.Length; i++)
            {
                worksheet.Cells[row, i + 1].Value = sample[i];
                worksheet.Cells[row, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 255, 220));
                worksheet.Cells[row, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            }
            row++;

            // Thêm 99 hàng trống (tổng 100 hàng dữ liệu)
            int dataEndRow = row + 98; // 99 hàng trống (từ row hiện tại đến row + 98)
            for (int dataRow = row; dataRow <= dataEndRow; dataRow++)
            {
                for (int col = 1; col <= 9; col++)
                {
                    worksheet.Cells[dataRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[dataRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                }
            }

            // ===== GHI CHÚ BÊN PHẢI BẢNG =====
            int noteCol = 11; // Cột K (cột 11)
            int noteRow = headerRow; // Bắt đầu từ hàng header

            worksheet.Cells[noteRow, noteCol].Value = "Ghi chú về cấu trúc file:";
            worksheet.Cells[noteRow, noteCol, noteRow, noteCol + 3].Merge = true;
            worksheet.Cells[noteRow, noteCol].Style.Font.Bold = true;
            worksheet.Cells[noteRow, noteCol].Style.Font.Size = 12;
            worksheet.Cells[noteRow, noteCol].Style.Font.Color.SetColor(Color.DarkRed);
            worksheet.Cells[noteRow, noteCol].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[noteRow, noteCol].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
            worksheet.Cells[noteRow, noteCol].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            noteRow++;

            string[] notes = {
                "- Cột 1 (STT): Bỏ qua, hệ thống tự tạo.",
                "- Cột 2 (Mã giảng viên): Bắt buộc, duy nhất.",
                "- Cột 3 (Họ): Bắt buộc.",
                "- Cột 4 (Tên): Bắt buộc.",
                "- Cột 5 (Giới tính): Nam/Nữ hoặc True/False hoặc 1/0.",
                "- Cột 6 (Ngày sinh): Định dạng dd/MM/yyyy hoặc yyyy-MM-dd.",
                "- Cột 7 (Email): Tùy chọn.",
                "- Cột 8 (Số điện thoại): Tùy chọn.",
                "- Cột 9 (Mã khoa): Bắt buộc."
            };

            foreach (var note in notes)
            {
                worksheet.Cells[noteRow, noteCol].Value = note;
                worksheet.Cells[noteRow, noteCol, noteRow, noteCol + 3].Merge = true;
                worksheet.Cells[noteRow, noteCol].Style.WrapText = true;
                worksheet.Cells[noteRow, noteCol].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                worksheet.Cells[noteRow, noteCol].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                noteRow++;
            }

            // Đặt độ rộng cột cho phần ghi chú
            worksheet.Column(noteCol).Width = 15;
            worksheet.Column(noteCol + 1).Width = 15;
            worksheet.Column(noteCol + 2).Width = 15;
            worksheet.Column(noteCol + 3).Width = 15;

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.View.FreezePanes(1, 1);
            worksheet.View.ZoomScale = 110;

            return package.GetAsByteArray();
        }
    }
}
