# Hỗ trợ LaTeX trong Hệ thống Thi

## Tổng quan
Hệ thống thi đã được tích hợp hỗ trợ LaTeX để hiển thị công thức toán học một cách chính xác và đẹp mắt.

## Cách sử dụng LaTeX

### 1. Công thức trong dòng (Inline)
- Sử dụng `\(...\)` hoặc `$...$`
- Ví dụ: `\(x^2 + y^2 = z^2\)` hoặc `$a + b = c$`

### 2. Công thức riêng dòng (Display)
- Sử dụng `\[...\]` hoặc `$$...$$`
- Ví dụ: `\[E = mc^2\]` hoặc `$$\int_{-\infty}^{\infty} e^{-x^2} dx = \sqrt{\pi}$$`

## Các ký hiệu LaTeX phổ biến

### Toán học cơ bản
- `x^2` → x² (lũy thừa)
- `x_2` → x₂ (chỉ số dưới)
- `\frac{a}{b}` → a/b (phân số)
- `\sqrt{x}` → √x (căn bậc hai)
- `\sum_{i=1}^{n}` → Σ (tổng)
- `\int_{a}^{b}` → ∫ (tích phân)

### Ký hiệu đặc biệt
- `\alpha`, `\beta`, `\gamma` → α, β, γ
- `\pi` → π
- `\infty` → ∞
- `\pm` → ±
- `\leq`, `\geq` → ≤, ≥

## Cách hoạt động

1. **MathJax Service**: Service C# gọi JavaScript MathJax
2. **QuestionItem Component**: Tự động phát hiện và xử lý LaTeX
3. **Typeset**: MathJax render công thức sau khi DOM được tạo

## Cấu hình

### MathJax được load từ CDN
```html
<script id="MathJax-script" async
        src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js">
</script>
```

### Service đăng ký trong Program.cs
```csharp
builder.Services.AddScoped<IMathJaxService, MathJaxService>();
```

## Xử lý lỗi

- Nếu MathJax không load được, công thức sẽ hiển thị dưới dạng text gốc
- Các lỗi được log vào console để debug

## Ví dụ sử dụng

### Trong câu hỏi
```
Câu 1: Giải phương trình bậc hai \(ax^2 + bx + c = 0\)

Công thức nghiệm: \[x = \frac{-b \pm \sqrt{b^2 - 4ac}}{2a}\]
```

### Trong đáp án
```
A) \(x = 2\)
B) \(x = -3\)
C) \(x = \frac{1}{2}\)
D) \(x = \sqrt{5}\)
```

## Lưu ý

1. **Performance**: MathJax chỉ render sau khi DOM được tạo hoàn toàn
2. **Compatibility**: Hỗ trợ cả inline và display math
3. **Fallback**: Nếu LaTeX lỗi, hiển thị text gốc
4. **Mobile**: Responsive và hoạt động tốt trên mobile

## Troubleshooting

### LaTeX không render
1. Kiểm tra console có lỗi JavaScript không
2. Đảm bảo MathJax đã load xong
3. Kiểm tra syntax LaTeX có đúng không

### Công thức bị cắt
1. Kiểm tra CSS overflow
2. Đảm bảo container đủ rộng
3. Sử dụng display math cho công thức dài

## Tài liệu tham khảo

- [MathJax Documentation](https://docs.mathjax.org/)
- [LaTeX Math Symbols](https://oeis.org/wiki/List_of_LaTeX_mathematical_symbols)
- [MathJax Examples](https://math.meta.stackexchange.com/questions/5020/mathjax-basic-tutorial-and-quick-reference)
