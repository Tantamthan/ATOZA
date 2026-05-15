using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATOZA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamAttemptsVersioningAndTeacherApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_ExamId",
                table: "Submissions");

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Exams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParentExamId",
                table: "Exams",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "Exams",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ExamAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAttempts_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamAttempts_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: -1,
                column: "ApprovalStatus",
                value: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ExamId_StudentId",
                table: "Submissions",
                columns: new[] { "ExamId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ParentExamId",
                table: "Exams",
                column: "ParentExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_ExamId_StudentId_Status",
                table: "ExamAttempts",
                columns: new[] { "ExamId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_StudentId",
                table: "ExamAttempts",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_Exams_ParentExamId",
                table: "Exams",
                column: "ParentExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exams_Exams_ParentExamId",
                table: "Exams");

            migrationBuilder.DropTable(
                name: "ExamAttempts");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_ExamId_StudentId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Exams_ParentExamId",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "ParentExamId",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "Exams");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ExamId",
                table: "Submissions",
                column: "ExamId");
        }
    }
}
