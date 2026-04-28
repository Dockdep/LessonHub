using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LessonsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageToLearnAndUseNativeLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LanguageToLearn",
                table: "LessonPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseNativeLanguage",
                table: "LessonPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LanguageToLearn",
                table: "LessonPlans");

            migrationBuilder.DropColumn(
                name: "UseNativeLanguage",
                table: "LessonPlans");
        }
    }
}
