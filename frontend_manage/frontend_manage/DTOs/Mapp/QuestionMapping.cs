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
    }
}
