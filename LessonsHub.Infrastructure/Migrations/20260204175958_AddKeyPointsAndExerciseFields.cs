using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LessonsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKeyPointsAndExerciseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Url",
                table: "Books");

            migrationBuilder.RenameColumn(
                name: "GeminiPrompt",
                table: "Lessons",
                newName: "KeyPoints");

            migrationBuilder.AddColumn<string>(
                name: "LessonTopic",
                table: "Lessons",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LessonType",
                table: "Lessons",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "Exercises",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AccuracyLevel",
                table: "ExerciseAnswers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewText",
                table: "ExerciseAnswers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LessonTopic",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "LessonType",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "AccuracyLevel",
                table: "ExerciseAnswers");

            migrationBuilder.DropColumn(
                name: "ReviewText",
                table: "ExerciseAnswers");

            migrationBuilder.RenameColumn(
                name: "KeyPoints",
                table: "Lessons",
                newName: "GeminiPrompt");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Books",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
