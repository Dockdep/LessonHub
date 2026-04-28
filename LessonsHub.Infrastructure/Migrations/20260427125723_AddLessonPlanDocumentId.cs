using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LessonsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonPlanDocumentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DocumentId",
                table: "LessonPlans",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlans_DocumentId",
                table: "LessonPlans",
                column: "DocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_LessonPlans_Documents_DocumentId",
                table: "LessonPlans",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LessonPlans_Documents_DocumentId",
                table: "LessonPlans");

            migrationBuilder.DropIndex(
                name: "IX_LessonPlans_DocumentId",
                table: "LessonPlans");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "LessonPlans");
        }
    }
}
