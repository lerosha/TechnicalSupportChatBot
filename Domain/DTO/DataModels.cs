using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DTO
{
    [Table("questionAnswerMapping")]
    public class QuestionAnswerMapping
    {
        [Column("mappingID")]
        [Key]
        public int MappingId { get; set; }

        [Column("prevQuestionID")]
        [ForeignKey("prevQuestionID")]
        public int PreviousQuestionId { get; set; }

        public Question PreviousQuestion { get; set; }

        [Column("useranswerID")]
        [ForeignKey("useranswerID")]
        public int AnswerId { get; set; }
        public Answer Answer { get; set; }

        [Column("nextQuestionID")]
        [ForeignKey("nextQuestionID")]
        public int? NextQuestionId { get; set; }
        public Question? NextQuestion { get; set; }
        [Column("solutionid")]
        [ForeignKey("solutionid")]
        public int? SolutionId { get; set; }
        public Solution? Solution { get; set; }
    }

    [Table("questions")]
    public class Question
    {
        [Key]
        [Column("questionID")]
        public int QuestionId { get; set; }
        [Column("text")]
        public string Text { get; set; }

        public List<QuestionAnswerMapping> NextQuestions { get; set; }
        public List<Answer> Answers { get; set; }
    }

    [Table("answers")]
    public class Answer
    {
        [Key]
        [Column("answerID")]
        public int AnswerId { get; set; }
        [Column("text")]
        public string Text { get; set; }
        [Column("questionid")]
        [ForeignKey("questionid")]
        public int QuestionId { get; set; }
        public Question Question { get; set; }
        public List<QuestionAnswerMapping> Questions { get; set; }
    }
    
    [Table("solutions")]
    public class Solution
    {
        [Key]
        [Column("solutionid")]
        public int SolutionId { get; set; }
        [Column("text")]
        public string Text { get; set; }

        public List<QuestionAnswerMapping> QuestionAnswerMappings { get; set; }
        [Column("documentationpath")]
        public string? DocPath { get; set; }
    }
}
