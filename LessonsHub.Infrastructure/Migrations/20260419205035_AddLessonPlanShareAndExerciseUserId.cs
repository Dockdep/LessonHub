using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LessonsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonPlanShareAndExerciseUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add Exercises.UserId as nullable, backfill from owning plan, then enforce NOT NULL.
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""Exercises"" e
                SET ""UserId"" = lp.""UserId""
                FROM ""Lessons"" l
                INNER JOIN ""LessonPlans"" lp ON lp.""Id"" = l.""LessonPlanId""
                WHERE e.""LessonId"" = l.""Id"" AND lp.""UserId"" IS NOT NULL;
            ");

            // Drop orphan exercises (lesson without an owning plan) — shouldn't exist but safe.
            migrationBuilder.Sql(@"
                DELETE FROM ""Exercises""
                WHERE ""UserId"" IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Exercises",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "LessonPlanShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LessonPlanId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonPlanShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonPlanShares_LessonPlans_LessonPlanId",
                        column: x => x.LessonPlanId,
                        principalTable: "LessonPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonPlanShares_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_UserId",
                table: "Exercises",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlanShares_LessonPlanId_UserId",
                table: "LessonPlanShares",
                columns: new[] { "LessonPlanId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonPlanShares_UserId",
                table: "LessonPlanShares",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Users_UserId",
                table: "Exercises",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Users_UserId",
                table: "Exercises");

            migrationBuilder.DropTable(
                name: "LessonPlanShares");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_UserId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Exercises");
        }
    }
}
