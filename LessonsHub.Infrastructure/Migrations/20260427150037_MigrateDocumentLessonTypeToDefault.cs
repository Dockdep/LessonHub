using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LessonsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Drops "Document" as a discrete LessonType. Affected lessons keep their
    /// LessonPlan.DocumentId (so RAG grounding still works) but the agent
    /// now uses the "Default" persona. LessonType lives only on the Lessons
    /// table — LessonPlan has no such column. Down is intentionally a no-op:
    /// once collapsed to Default we can't know which were originally Document
    /// vs. always-Default-with-attached-doc.
    /// </summary>
    public partial class MigrateDocumentLessonTypeToDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Lessons\" SET \"LessonType\" = 'Default' WHERE \"LessonType\" = 'Document';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Lossy direction — see class summary.
        }
    }
}
