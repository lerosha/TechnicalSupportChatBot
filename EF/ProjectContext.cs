using Microsoft.EntityFrameworkCore;
using Domain.DTO;

namespace EF
{
    public class ProjectContext : DbContext
    {
        public ProjectContext(DbContextOptions<ProjectContext> options) : base(options)
        {
        }

        // Определение свойств DbSet для каждой таблицы в БД
        public DbSet<QuestionAnswerMapping> QuestionAnswerMappings { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Solution> Solutions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Определение связей
            modelBuilder.Entity<QuestionAnswerMapping>()
                .HasOne(qam => qam.PreviousQuestion)
                .WithMany(q => q.NextQuestions)
                .HasForeignKey(qam => qam.PreviousQuestionId)
                .OnDelete(DeleteBehavior.Restrict);  

            modelBuilder.Entity<QuestionAnswerMapping>()
                .HasOne(qam => qam.Answer)
                .WithMany(a => a.Questions)
                .HasForeignKey(qam => qam.AnswerId)
                .OnDelete(DeleteBehavior.Restrict);
             
            modelBuilder.Entity<QuestionAnswerMapping>()
                .HasOne(qam => qam.NextQuestion)
                .WithMany()
                .HasForeignKey(qam => qam.NextQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<QuestionAnswerMapping>()
                .HasOne(qam => qam.Solution)
                .WithMany(s => s.QuestionAnswerMappings)
                .HasForeignKey(qam => qam.SolutionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<Solution>()
                .HasMany(s => s.QuestionAnswerMappings)
                .WithOne(qam => qam.Solution)
                .HasForeignKey(qam => qam.SolutionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}
