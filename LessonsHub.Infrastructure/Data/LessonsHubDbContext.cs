using System.Text.Json;
using LessonsHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LessonsHub.Infrastructure.Data;

public class LessonsHubDbContext : DbContext
{
    public LessonsHubDbContext(DbContextOptions<LessonsHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<LessonDay> LessonDays { get; set; }
    public DbSet<LessonPlan> LessonPlans { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<ExerciseAnswer> ExerciseAnswers { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Documentation> Documentation { get; set; }
    public DbSet<AiRequestLog> AiRequestLogs { get; set; }
    public DbSet<LessonPlanShare> LessonPlanShares { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Job> Jobs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GoogleId).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.GoogleId).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PictureUrl).HasMaxLength(500);
            entity.Property(e => e.GoogleApiKey).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // LessonDay configuration
        modelBuilder.Entity<LessonDay>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
        });

        // LessonPlan configuration
        modelBuilder.Entity<LessonPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Topic).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description);
            entity.Property(e => e.NativeLanguage).HasMaxLength(100);
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.HasOne(e => e.User)
                .WithMany(u => u.LessonPlans)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // Optional reference to a user-uploaded source Document (Document
            // lesson type). SetNull on Document delete: removing the source
            // doesn't cascade-destroy the plan, but the plan loses the link.
            entity.HasOne(e => e.Document)
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        // Lesson configuration
        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LessonNumber).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ShortDescription).HasMaxLength(1000);
            entity.Property(e => e.Content);
            entity.Property(e => e.LessonType).HasMaxLength(200);
            entity.Property(e => e.LessonTopic).HasMaxLength(500);
            var keyPointsConverter = new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );
            var keyPointsComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()
            );
            entity.Property(e => e.KeyPoints)
                .HasConversion(keyPointsConverter)
                .Metadata.SetValueComparer(keyPointsComparer);

            entity.Property(e => e.IsCompleted).HasDefaultValue(false);
            entity.Property(e => e.CompletedAt);

            // Relationship with LessonPlan
            entity.HasOne(e => e.LessonPlan)
                .WithMany(lp => lp.Lessons)
                .HasForeignKey(e => e.LessonPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with LessonDay
            entity.HasOne(e => e.LessonDay)
                .WithMany(ld => ld.Lessons)
                .HasForeignKey(e => e.LessonDayId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Exercise configuration
        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExerciseText).IsRequired();
            entity.Property(e => e.Difficulty).HasMaxLength(50);
            entity.Property(e => e.UserId).IsRequired();

            // Relationship with Lesson
            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.Exercises)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExerciseAnswer configuration
        modelBuilder.Entity<ExerciseAnswer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserResponse).IsRequired();
            entity.Property(e => e.SubmittedAt).IsRequired();
            entity.Property(e => e.AccuracyLevel);
            entity.Property(e => e.ReviewText);

            // Relationship with Exercise
            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.Answers)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Text).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Optional relationships - one or the other will be set
            entity.HasOne<Lesson>()
                .WithMany(l => l.ChatHistory)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            entity.HasOne<Exercise>()
                .WithMany(ex => ex.ChatHistory)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        });

        // Video configuration
        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Channel).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);

            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.Videos)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Book configuration
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Author).IsRequired().HasMaxLength(200);
            entity.Property(e => e.BookName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ChapterName).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000);

            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.Books)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Documentation configuration
        modelBuilder.Entity<Documentation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Section).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);

            entity.HasOne(e => e.Lesson)
                .WithMany(l => l.Documentation)
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LessonPlanShare configuration
        modelBuilder.Entity<LessonPlanShare>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SharedAt).IsRequired();
            entity.HasIndex(e => new { e.LessonPlanId, e.UserId }).IsUnique();

            entity.HasOne(e => e.LessonPlan)
                .WithMany(lp => lp.Shares)
                .HasForeignKey(e => e.LessonPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).HasMaxLength(200);
            entity.Property(e => e.StorageUri).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.IngestionStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IngestionError).HasMaxLength(2000);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
        });

        // Job configuration
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<int>().IsRequired();
            entity.Property(e => e.PayloadJson).IsRequired();
            entity.Property(e => e.IdempotencyKey).HasMaxLength(200);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(100);
            entity.Property(e => e.Error).HasMaxLength(4000);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Hot path: "what jobs are still in flight for this user?"
            entity.HasIndex(e => new { e.UserId, e.Status });

            // Idempotency: same (user, type, key) → same job. Filtered so NULL
            // keys (no client opt-in) don't collide.
            entity.HasIndex(e => new { e.UserId, e.Type, e.IdempotencyKey })
                .IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL");
        });

        // AiRequestLog configuration
        modelBuilder.Entity<AiRequestLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestType).HasMaxLength(100);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.FinishReason).HasMaxLength(100);
            
            entity.Property(e => e.PricePerIn).HasColumnType("decimal(18,8)");
            entity.Property(e => e.PricePerOut).HasColumnType("decimal(18,8)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,8)");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
