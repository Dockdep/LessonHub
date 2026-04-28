using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LessonsHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToLessonDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add column as nullable so we can backfill before enforcing NOT NULL.
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "LessonDays",
                type: "integer",
                nullable: true);

            // 2. Backfill UserId from any related lesson's owning user.
            //    Assumes existing LessonDays are not shared across users (single-user dataset).
            //    If a LessonDay was shared, the unique index in step 5 will surface the conflict.
            migrationBuilder.Sql(@"
                UPDATE ""LessonDays"" ld
                SET ""UserId"" = sub.""UserId""
                FROM (
                    SELECT DISTINCT ON (l.""LessonDayId"") l.""LessonDayId"", lp.""UserId""
                    FROM ""Lessons"" l
                    INNER JOIN ""LessonPlans"" lp ON lp.""Id"" = l.""LessonPlanId""
                    WHERE l.""LessonDayId"" IS NOT NULL AND lp.""UserId"" IS NOT NULL
                ) sub
                WHERE ld.""Id"" = sub.""LessonDayId"";
            ");

            // 3. Delete orphan LessonDays that have no lessons (no way to attribute them).
            migrationBuilder.Sql(@"
                DELETE FROM ""LessonDays""
                WHERE ""UserId"" IS NULL;
            ");

            // 4. Enforce NOT NULL.
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "LessonDays",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // 5. Composite unique index — at most one LessonDay per (user, date).
            migrationBuilder.CreateIndex(
                name: "IX_LessonDays_UserId_Date",
                table: "LessonDays",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonDays_Users_UserId",
                table: "LessonDays",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LessonDays_Users_UserId",
                table: "LessonDays");

            migrationBuilder.DropIndex(
                name: "IX_LessonDays_UserId_Date",
                table: "LessonDays");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "LessonDays");
        }
    }
}
