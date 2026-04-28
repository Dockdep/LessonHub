using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LessonsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Lessons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Lessons",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Lessons");
        }
    }
}
