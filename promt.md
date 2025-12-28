ĐỀ MẪU – SPEC CUỐI CÙNG (HOÁN VỊ + IMAGE + AUDIO + ANSWER + MATCH)
Ghi chú: File này dùng để test parser. Ảnh chèn ngay dưới [image]. Audio chỉ đánh dấu bằng [audio]. Có đầy đủ dạng: MCQ, LATEX, AUDIO, SHORT, MATCH.
[question id="Q1", exam permute=true]
When did the woman put her keys in her purse?
A. When she came home
B. When she was driving the car
C. When she left school
D. When she opened the front door
[answer]A[/answer]
[/question]

[question id="Q2", exam permute=true]
Galileo ______ his first telescope in 1609.
A. has built
B. had built
C. built
D. builds
[answer]C[/answer]
[/question]

[question id="Q3", exam permute=true]
Chuyển tích phân sau sang tọa độ trụ:
[latex]\iiint_V f(x,y,z)\, dV[/latex]
A. ...
B. ...
C. ...
D. ...
[answer]B[/answer]
[/question]

[question id="Q4"]
Nhỏ vài giọt dung dịch iot vào mặt cắt của một lát bánh mì như hình.
[Image]
A. Iot và glucozo
B. Iot và tinh bột
C. ...
D. ...
[answer]B[/answer]
[/question]

[question id="Q5", exam permute=true]
Cho miền Ω như hình, tính:
[Image]
[latex]\iint_{\Omega} f(x,y)\, dxdy[/latex]
A. ...
B. ...
C. ...
D. ...
[answer]A[/answer]
[/question]

[question id="Q6", exam permute=true]
[audio]
What does the man keep in his wallet?
A. ID card
B. Cash
C. Credit cards
D. All are correct
[answer]D[/answer]
[/question]

[question id="Q7", exam permute=true]
[audio]
What does the man keep in his wallet?
A. Sound
B. Correct
C. Nouod
D. All are correct
[answer]B[/answer]
[/question]


[question id="Q8", exam permute=true]
Giải thích hiện tượng đổi màu khi nhỏ iot vào bánh mì.
[answer]Iot phản ứng với tinh bột tạo màu xanh tím.[/answer]
[/question]


[question id="Q9", exam permute=true]
Which sentence is correct?
A. She don’t like coffee.
B. She doesn’t likes coffee.
C. She doesn’t like coffee.
D. She not like coffee.
[answer]C[/answer]
[/question]

[question id="Q10", exam permute=true]
Tính đạo hàm:
[latex]\frac{d}{dx}(x^2 + 3x)[/latex]
A. 2x + 3
B. x + 3
C. 2x
D. x^2
[answer]A[/answer]
[/question]

[question id="Q11", exam permute=true]
Viết công thức tính vận tốc trung bình.
[answer]v_tb = Δs / Δt[/answer]
[/question]

[question id="Q12", exam permute=true]
Which is a renewable energy source?
A. Coal
B. Oil
C. Wind
D. Gas
[answer]C[/answer]
[/question]

[question id="Q13", exam permute=true]
[audio]
What time is the meeting?
A. 8 AM
B. 9 AM
C. 10 AM
D. 11 AM
[answer]B[/answer]
[/question]

[question id="Q14", exam permute=true]
Choose the correct passive form:
They built the house in 1990.
A. The house is built in 1990.
B. The house was built in 1990.
C. The house has built in 1990.
D. The house is building in 1990.
[answer]B[/answer]
[/question]

[question id="Q15", exam permute=true]
[latex]\int_0^1 x dx[/latex]
A. 1
B. 1/2
C. 0
D. 2
[answer]B[/answer]
[/question]

[question id="Q16", exam permute=true]
Nêu định nghĩa phản ứng hóa học.
[answer]Quá trình biến đổi chất ban đầu thành chất mới.[/answer]
[/question]

[question id="Q17", exam permute=true]
Which one is an adjective?
A. Run
B. Quickly
C. Happy
D. Eat
[answer]C[/answer]
[/question]

[question id="Q18", exam permute=true]
What is the capital of France?
A. Berlin
B. Madrid
C. Paris
D. Rome
[answer]C[/answer]
[/question]

Cách thêm audio và image:
ExamPackage.zip
 ├── exam.docx
 ├── Audio/
 │    └── Q6.mp3
 └── Images/
      └── Q4.png

Quy tắc xử lý:
1. Audio: Tạo folder Audio ở wwwroot, lưu file audio vào đó. Thẻ [audio] trong câu hỏi sẽ được thay thế bằng trình phát âm thanh. Tên file audio trùng với ID câu hỏi (ví dụ: Q6.mp3 cho câu hỏi Q6).
2. Image: Tạo folder Images ở wwwroot, lưu file ảnh vào đó. Thẻ [image] trong nội dung câu hỏi là vị trí hiển thị ảnh. Tên file ảnh tương ứng với ID câu hỏi (ví dụ: Q4.png cho câu hỏi Q4).

Đây là cách import file word bằng zip file

