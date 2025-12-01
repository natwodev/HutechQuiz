using frontend_manage.DTOs;

namespace frontend_manage.Services
{
    public class ExamMockService
    {
        public StartExamResponseDto GetMockExam()
        {
            var exam = new ShuffledExamPaperDto
            {
                ShuffledExamPaperId = 1,
                ShuffledExamPaperCore = "A",
                Title = "Đề thi mẫu",
                OriginalExamPaperId = 10,
                SubjectId = 5,
                IsApproved = true,
                AnswerKey = string.Empty,
                SubjectName = "Toán cao cấp",
                SubjectCode = "MATH101",
                QuestionStructures = new List<QuestionStructureDto>()
            };

            // Câu hỏi đơn (single choice) có LaTeX
            exam.QuestionStructures.Add(new QuestionStructureDto
            {
                OriginalExamPaperDetailId = 101,
                Order = 1,
                QuestionContent = "Tính giá trị của [latex]\\(\\int_0^1 x^2 dx\\)[/latex] là bao nhiêu?",
                Answers = new List<AnswerStructureDto>
                {
                    new AnswerStructureDto{ AnswerId=1, Order=1, AnswerContent="0", OriginalExamPaperDetailId=101},
                    new AnswerStructureDto{ AnswerId=2, Order=2, AnswerContent="1/2", OriginalExamPaperDetailId=101},
                    new AnswerStructureDto{ AnswerId=3, Order=3, AnswerContent="1/3", OriginalExamPaperDetailId=101},
                    new AnswerStructureDto{ AnswerId=4, Order=4, AnswerContent="1", OriginalExamPaperDetailId=101},
                }
            });

            // Câu hỏi nhóm: một câu cha và hai câu con
            var group = new QuestionStructureDto
            {
                OriginalExamPaperDetailId = 200,
                Order = 2,
                QuestionContent = "[group] Cho ma trận [latex]\\(A=\\begin{bmatrix}1&2\\\\0&1\\end{bmatrix}\\)[/latex]. Trả lời câu hỏi sau:",
            };
            group.ChildQuestions.Add(new QuestionStructureDto
            {
                OriginalExamPaperDetailId = 201,
                Order = 1,
                ParentQuestionId = 200,
                QuestionContent = "Giá trị [latex]\\(\\det(A)\\)[/latex] là?",
                Answers = new List<AnswerStructureDto>
                {
                    new AnswerStructureDto{ AnswerId=21, Order=1, AnswerContent="1", OriginalExamPaperDetailId=201},
                    new AnswerStructureDto{ AnswerId=22, Order=2, AnswerContent="2", OriginalExamPaperDetailId=201},
                    new AnswerStructureDto{ AnswerId=23, Order=3, AnswerContent="3", OriginalExamPaperDetailId=201},
                }
            });
            group.ChildQuestions.Add(new QuestionStructureDto
            {
                OriginalExamPaperDetailId = 202,
                Order = 2,
                ParentQuestionId = 200,
                QuestionContent = "Hạng của [latex]\\(A\\)[/latex] là?",
                Answers = new List<AnswerStructureDto>
                {
                    new AnswerStructureDto{ AnswerId=31, Order=1, AnswerContent="1", OriginalExamPaperDetailId=202},
                    new AnswerStructureDto{ AnswerId=32, Order=2, AnswerContent="2", OriginalExamPaperDetailId=202},
                }
            });
            exam.QuestionStructures.Add(group);

            // Câu hỏi nối: thêm keyword [matching]
            exam.QuestionStructures.Add(new QuestionStructureDto
            {
                OriginalExamPaperDetailId = 300,
                Order = 3,
                QuestionContent = "[matching] Ghép công thức với tên gọi tương ứng",
                Answers = new List<AnswerStructureDto>
                {
                    new AnswerStructureDto{ AnswerId=41, Order=1, AnswerContent="[latex]\\(a^2+b^2=c^2\\)[/latex]", OriginalExamPaperDetailId=300},
                    new AnswerStructureDto{ AnswerId=42, Order=2, AnswerContent="[latex]\\(e^{i\\\\pi}+1=0\\)[/latex]", OriginalExamPaperDetailId=300},
                    new AnswerStructureDto{ AnswerId=43, Order=3, AnswerContent="Định lý Pythagoras", OriginalExamPaperDetailId=300},
                    new AnswerStructureDto{ AnswerId=44, Order=4, AnswerContent="Đồng nhất thức Euler", OriginalExamPaperDetailId=300},
                }
            });

            // Thêm nhiều câu hỏi trắc nghiệm đơn để tiện kiểm thử giao diện (tổng khoảng 30 câu)
            int nextDetailId = 400;
            int nextOrder = 4;
            int nextAnswerId = 1000;
            // Thêm 4 câu trắc nghiệm đơn (cộng với 1 câu đã có ở trên = 5 câu thường)
            for (int i = 1; i <= 4; i++)
            {
                var q = new QuestionStructureDto
                {
                    OriginalExamPaperDetailId = nextDetailId,
                    Order = nextOrder,
                    QuestionContent = $"Câu kiểm thử {i}: Chọn giá trị đúng của [latex]\\({i} + {i}\\)[/latex]?",
                    Answers = new List<AnswerStructureDto>
                    {
                        new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=1, AnswerContent=$"{i}", OriginalExamPaperDetailId=nextDetailId},
                        new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=2, AnswerContent=$"{i*2}", OriginalExamPaperDetailId=nextDetailId},
                        new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=3, AnswerContent=$"{i+1}", OriginalExamPaperDetailId=nextDetailId},
                        new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=4, AnswerContent=$"{i*2+1}", OriginalExamPaperDetailId=nextDetailId},
                    }
                };
                exam.QuestionStructures.Add(q);
                nextDetailId++;
                nextOrder++;
            }

            // Một câu hỏi nối thứ hai để kiểm thử LeaderLine
            exam.QuestionStructures.Add(new QuestionStructureDto
            {
                OriginalExamPaperDetailId = nextDetailId,
                Order = nextOrder,
                QuestionContent = "[matching] Ghép quốc gia với thủ đô",
                Answers = new List<AnswerStructureDto>
                {
                    new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=1, AnswerContent="Việt Nam", OriginalExamPaperDetailId=nextDetailId},
                    new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=2, AnswerContent="Pháp", OriginalExamPaperDetailId=nextDetailId},
                    new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=3, AnswerContent="Hà Nội", OriginalExamPaperDetailId=nextDetailId},
                    new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=4, AnswerContent="Paris", OriginalExamPaperDetailId=nextDetailId},
                }
            });
            nextDetailId++; nextOrder++;

            // Các câu LaTeX phức tạp để stress-test KaTeX
            var complexLatex = new List<(string q, string a1, string a2, string a3, string a4)>
            {
                ("Tính đạo hàm của [latex]\\(f(x)=x^x\\)[/latex]", "[latex]f'(x) = x^x(\\ln x + 1)[/latex]", "[latex]f'(x)=x^{x-1}[/latex]", "[latex]f'(x)=\\ln(x)[/latex]", "[latex]f'(x)=x^x[/latex]"),
                ("Giá trị của [latex]\\(\\displaystyle\\int_0^{\\pi} \\sin x\\,dx\\)[/latex] là", "[latex]2[/latex]", "[latex]1[/latex]", "[latex]0[/latex]", "[latex]\\pi[/latex]"),
                ("Giới hạn [latex]\\(\\displaystyle\\lim_{x\\to 0} \\frac{\\sin x}{x}\\)[/latex]", "[latex]1[/latex]", "[latex]0[/latex]", "[latex]\\infty[/latex]", "[latex]-1[/latex]"),
                ("Giá trị tổng [latex]\\(\\displaystyle\\sum_{k=1}^{n} k = ?\\)[/latex]", "[latex]\\frac{n(n+1)}{2}[/latex]", "[latex]n^2[/latex]", "[latex]n(n-1)[/latex]", "[latex]\\frac{n^2}{2}[/latex]"),
                ("Ma trận nghịch đảo của [latex]\\(\\begin{pmatrix}1&2\\\\0&1\\end{pmatrix}\\)[/latex] là", "[latex]\\(\\begin{pmatrix}1&-2\\\\0&1\\end{pmatrix}\\)[/latex]", "[latex]\\(\\begin{pmatrix}1&2\\\\0&1\\end{pmatrix}\\)[/latex]", "[latex]\\(\\begin{pmatrix}1&0\\\\-2&1\\end{pmatrix}\\)[/latex]", "[latex]\\(\\begin{pmatrix}0&1\\\\1&0\\end{pmatrix}\\)[/latex]"),
                ("Hàm từng phần [latex]\\(f(x)=\\(\\begin{cases}x^2,& x\\ge 0\\\\-x,& x<0\\end{cases}\\)\\)[/latex] liên tục tại 0?", "Có", "Không", "Chỉ bên trái", "Chỉ bên phải"),
                ("Khai triển Taylor của [latex]\\(\\sin x\\)[/latex] quanh 0 đến bậc 5 là", "[latex]x-\\frac{x^3}{3!}+\\frac{x^5}{5!}[/latex]", "[latex]x+\\frac{x^3}{3!}[/latex]", "[latex]1-\\frac{x^2}{2}[/latex]", "[latex]0[/latex]"),
                ("Tính [latex]\\(\\displaystyle\\int \\frac{1}{1+x^2}dx\\)[/latex]", "[latex]\\arctan x + C[/latex]", "[latex]\\ln(1+x^2)+C[/latex]", "[latex]x+C[/latex]", "[latex]-\\arctan x + C[/latex]"),
                ("Nghiệm của [latex]\\(e^x=2\\)[/latex]", "[latex]\\ln 2[/latex]", "[latex]2\\ln2[/latex]", "[latex]1[/latex]", "[latex]2[/latex]"),
                ("Tổng hình học [latex]\\(1+q+q^2+...+q^{n}\\)[/latex] bằng", "[latex]\\frac{1-q^{n+1}}{1-q}[/latex]", "[latex]\\frac{q^{n}-1}{q-1}[/latex]", "[latex]nq[/latex]", "[latex]\\frac{1}{1-q}[/latex]"),
                ("Khoảng cách hai điểm [latex]\\((x_1,y_1),(x_2,y_2)\\)[/latex]", "[latex]\\sqrt{(x_2-x_1)^2+(y_2-y_1)^2}[/latex]", "[latex]|x_2-x_1|+|y_2-y_1|[/latex]", "[latex](x_2-x_1)+(y_2-y_1)[/latex]", "[latex]x_2y_1-x_1y_2[/latex]"),
                ("Đạo hàm bậc hai của [latex]\\(x^3\\)[/latex]", "[latex]6x[/latex]", "[latex]3x^2[/latex]", "[latex]x^3[/latex]", "[latex]0[/latex]"),
                ("Tính [latex]\\(\\displaystyle\\int_0^1 x\\ln x\\,dx\\)[/latex]", "[latex]-\\frac{1}{4}[/latex]", "[latex]-\\frac{1}{2}[/latex]", "[latex]0[/latex]", "[latex]\\frac{1}{4}[/latex]"),
                ("Chuẩn hoá vector [latex]\\(\\vec{v}=(3,4)\\)[/latex]", "[latex]\\left(\\frac{3}{5},\\frac{4}{5}\\right)[/latex]", "[latex](3,4)[/latex]", "[latex](4,3)[/latex]", "[latex]\\left(\\frac{4}{5},\\frac{3}{5}\\right)[/latex]"),
                ("Số tổ hợp [latex]\\(C_n^k\\)[/latex] bằng", "[latex]\\frac{n!}{k!(n-k)!}[/latex]", "[latex]n^k[/latex]", "[latex]k^n[/latex]", "[latex]\\frac{k!}{n!(n-k)!}[/latex]"),
                ("Xác suất biến cố độc lập [latex]A,B[/latex] thoả [latex]P(A\\cap B)=?[/latex]", "[latex]P(A)P(B)[/latex]", "[latex]P(A)+P(B)[/latex]", "[latex]P(A|B)P(B)[/latex]", "[latex]P(A)+P(B)-P(A)P(B)[/latex]"),
                ("Giá trị riêng của ma trận [latex]\\(\\begin{pmatrix}2&0\\\\0&3\\end{pmatrix}\\)[/latex]", "[latex]2,3[/latex]", "[latex]0,1[/latex]", "[latex]-2,-3[/latex]", "[latex]1,2[/latex]"),
                ("Chuẩn hoá đa thức Legendre [latex]P_2(x)\\)[/latex]", "[latex]\\frac{1}{2}(3x^2-1)[/latex]", "[latex]x^2-1[/latex]", "[latex]3x^2[/latex]", "[latex]x^2+1[/latex]"),
                ("Biểu diễn số phức [latex]\\(z=re^{i\\theta}\\)[/latex]", "[latex]r(\\cos\\theta+i\\sin\\theta)[/latex]", "[latex]r(\\cos\\theta- i\\sin\\theta)[/latex]", "[latex]r\\cos\\theta[/latex]", "[latex]r\\sin\\theta[/latex]"),
                ("Định lý nhị thức [latex]\\((a+b)^n\\)[/latex]", "[latex]\\sum_{k=0}^n C_n^k a^{n-k}b^k[/latex]", "[latex]a^n+b^n[/latex]", "[latex]na^{n-1}b[/latex]", "[latex]C_n^1 ab[/latex]"),
                ("Chu kỳ của [latex]\\(\\sin x\\)[/latex]", "[latex]2\\pi[/latex]", "[latex]\\pi[/latex]", "[latex]1[/latex]", "[latex]4\\pi[/latex]"),
                ("Tính [latex]\\(\\displaystyle\\int e^{2x}dx\\)[/latex]", "[latex]\\frac{1}{2}e^{2x}+C[/latex]", "[latex]e^{2x}+C[/latex]", "[latex]2e^{x}+C[/latex]", "[latex]\\ln e^{2x}+C[/latex]"),
            };
            foreach (var (q, a1, a2, a3, a4) in complexLatex)
            {
                var qx = new QuestionStructureDto
                {
                    OriginalExamPaperDetailId = nextDetailId,
                    Order = nextOrder,
                    QuestionContent = q,
                    Answers = new List<AnswerStructureDto>
                    {
                        new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=1, AnswerContent=a1, OriginalExamPaperDetailId=nextDetailId},
                        new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=2, AnswerContent=a2, OriginalExamPaperDetailId=nextDetailId},
                        new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=3, AnswerContent=a3, OriginalExamPaperDetailId=nextDetailId},
                        new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=4, AnswerContent=a4, OriginalExamPaperDetailId=nextDetailId},
                    }
                };
                exam.QuestionStructures.Add(qx);
                nextDetailId++; nextOrder++;
            }

            // Bổ sung thêm 3 câu nối nữa (cộng với 2 câu nối đã có = 5 câu nối)
            for (int i = 1; i <= 3; i++)
            {
                exam.QuestionStructures.Add(new QuestionStructureDto
                {
                    OriginalExamPaperDetailId = nextDetailId,
                    Order = nextOrder,
                    QuestionContent = $"[matching] Ghép khái niệm #{i}",
                    Answers = new List<AnswerStructureDto>
                    {
                        new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=1, AnswerContent=$"Trái {i}-1", OriginalExamPaperDetailId=nextDetailId},
                        new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=2, AnswerContent=$"Trái {i}-2", OriginalExamPaperDetailId=nextDetailId},
                        new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=3, AnswerContent=$"Phải {i}-1", OriginalExamPaperDetailId=nextDetailId},
                        new AnswerStructureDto{ AnswerId=nextAnswerId++, Order=4, AnswerContent=$"Phải {i}-2", OriginalExamPaperDetailId=nextDetailId},
                    }
                });
                nextDetailId++; nextOrder++;
            }

            // Tính tổng câu hỏi hiển thị (bao gồm câu con)
            int totalCount = 0;
            foreach (var q in exam.QuestionStructures)
            {
                totalCount++;
                if (q.ChildQuestions != null) totalCount += q.ChildQuestions.Count;
            }

            var original = new OriginalExamPaperDto
            {
                OriginalExamPaperId = 10,
                OriginalExamPaperCore = "A",
                Title = "Đề thi mẫu",
                SubjectId = 5,
                DurationMinutes = 120,
                TotalQuestions = totalCount,
            };

            return new StartExamResponseDto
            {
                StudentSession = new StudentExamSessionCacheDto(),
                ExamPaper = exam,
                OriginalExamPaper = original
            };
        }
    }
}


