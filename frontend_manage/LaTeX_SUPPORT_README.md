# Hỗ trợ LaTeX (KaTeX)

Hệ thống đã chuyển sang dùng KaTeX để hiển thị công thức.

## Cách sử dụng

- Inline: `\(...\)` hoặc `$...$`
- Block: `\[...\]` hoặc `$$...$$`
- Có thể bọc bằng thẻ `[latex]...[/latex]` (bên trong dùng các delimiter trên).

## Cấu hình

KaTeX CDN đã được thêm vào `wwwroot/index.html` và auto-render được kích hoạt qua `wwwroot/js/katexInterop.js`.

## Ghi chú

- KaTeX render rất nhanh, không phụ thuộc vào MathJax.
- Nếu công thức không hiển thị, kiểm tra delimiter và console lỗi JS.
