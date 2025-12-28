using frontend_manage.DTOs;

namespace frontend_manage.DTOs.Mapp
{
    public static class QuestionMapping
    {
        public static QuestionStructureDto MapToQuestionStructure(OriginalExamPaperDetailDto originalQuestion)
        {
            if (originalQuestion == null)
                return null;

            var questionStructure = new QuestionStructureDto
            {
                OriginalExamPaperDetailId = originalQuestion.OriginalExamPaperDetailId,
                ParentQuestionId = originalQuestion.ParentQuestionId,
                Order = originalQuestion.Order,
                QuestionContent = originalQuestion.QuestionContent,
                ChildQuestions = new List<QuestionStructureDto>(),
                Answers = new List<AnswerStructureDto>()
            };

            // Map child questions recursively
            if (originalQuestion.ChildQuestions?.Any() == true)
            {
                questionStructure.ChildQuestions = originalQuestion.ChildQuestions
                    .Select(child => MapToQuestionStructure(child))
                    .Where(child => child != null)
                    .ToList();
            }

            // Map answers
            if (originalQuestion.Answers?.Any() == true)
            {
                questionStructure.Answers = originalQuestion.Answers
                    .Select(answer => MapToAnswerStructure(answer))
                    .Where(answer => answer != null)
                    .ToList();
            }

            return questionStructure;
        }
        
        public static AnswerStructureDto MapToAnswerStructure(AnswerDto originalAnswer)
        {
            if (originalAnswer == null)
                return null;

            return new AnswerStructureDto
            {
                AnswerId = originalAnswer.AnswerId,
                Order = originalAnswer.Order,
                AnswerContent = originalAnswer.AnswerContent,
                OriginalExamPaperDetailId = originalAnswer.OriginalExamPaperDetailId
            };
        }
        
        public static List<QuestionStructureDto> MapToQuestionStructureList(List<OriginalExamPaperDetailDto> originalQuestions)
        {
            if (originalQuestions?.Any() != true)
                return new List<QuestionStructureDto>();

            return originalQuestions
                .Select(question => MapToQuestionStructure(question))
                .Where(question => question != null)
                .ToList();
        }

        public static void EnrichQuestionStructuresWithContent(List<QuestionStructureDto> structures, List<OriginalExamPaperDetailDto> details)
        {
            if (structures == null || details == null) return;

            // Tạo flat dictionary để lookup cho nhanh
            var detailsMap = new Dictionary<int, OriginalExamPaperDetailDto>();
            foreach (var detail in details)
            {
                detailsMap[detail.OriginalExamPaperDetailId] = detail;
                if (detail.ChildQuestions != null)
                {
                    foreach (var child in detail.ChildQuestions)
                    {
                        detailsMap[child.OriginalExamPaperDetailId] = child;
                    }
                }
            }

            // Đệ quy để bổ sung nội dung
            foreach (var structure in structures)
            {
                EnrichStructure(structure, detailsMap);
            }
        }

        private static void EnrichStructure(QuestionStructureDto structure, Dictionary<int, OriginalExamPaperDetailDto> detailsMap)
        {
            if (detailsMap.TryGetValue(structure.OriginalExamPaperDetailId, out var detail))
            {
                structure.QuestionContent = detail.QuestionContent;

                // Bổ sung nội dung cho đáp án
                if (structure.Answers != null && detail.Answers != null)
                {
                    var answersMap = detail.Answers.ToDictionary(a => a.AnswerId);
                    foreach (var ansStructure in structure.Answers)
                    {
                        if (answersMap.TryGetValue(ansStructure.AnswerId, out var answerDto))
                        {
                            ansStructure.AnswerContent = answerDto.AnswerContent;
                        }
                    }
                }

                // Đệ quy cho câu hỏi con
                if (structure.ChildQuestions != null && structure.ChildQuestions.Count > 0)
                {
                    foreach (var childStructure in structure.ChildQuestions)
                    {
                        EnrichStructure(childStructure, detailsMap);
                    }
                }
            }
        }
    }
}
