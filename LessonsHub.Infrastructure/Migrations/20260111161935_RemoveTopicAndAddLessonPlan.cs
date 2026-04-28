using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LessonsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTopicAndAddLessonPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_LessonDays_LessonDayId",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Topics_TopicId",
                table: "Lessons");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.RenameColumn(
                name: "TopicId",
                table: "Lessons",
                newName: "LessonPlanId");

            migrationBuilder.RenameIndex(
                name: "IX_Lessons_TopicId",
                table: "Lessons",
                newName: "IX_Lessons_LessonPlanId");

            migrationBuilder.AlterColumn<string>(
                name: "ShortDescription",
                table: "Lessons",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "LessonPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonPlans", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_LessonDays_LessonDayId",
                table: "Lessons",
                column: "LessonDayId",
                principalTable: "LessonDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_LessonPlans_LessonPlanId",
                table: "Lessons",
                column: "LessonPlanId",
                principalTable: "LessonPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_LessonDays_LessonDayId",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_LessonPlans_LessonPlanId",
                table: "Lessons");

            migrationBuilder.DropTable(
                name: "LessonPlans");

            migrationBuilder.RenameColumn(
                name: "LessonPlanId",
                table: "Lessons",
                newName: "TopicId");

            migrationBuilder.RenameIndex(
                name: "IX_Lessons_LessonPlanId",
                table: "Lessons",
                newName: "IX_Lessons_TopicId");

            migrationBuilder.AlterColumn<string>(
                name: "ShortDescription",
                table: "Lessons",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_LessonDays_LessonDayId",
                table: "Lessons",
                column: "LessonDayId",
                principalTable: "LessonDays",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Topics_TopicId",
                table: "Lessons",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
