using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATOZA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueClassExamAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClassAssignments_ClassId",
                table: "ClassAssignments");

            // Xóa các bản ghi giao bài trùng (cùng ClassId + ExamId), giữ lại bản ghi có Id nhỏ nhất
            migrationBuilder.Sql(@"
                DELETE FROM ClassAssignments
                WHERE Id NOT IN (
                    SELECT MIN(Id)
                    FROM ClassAssignments
                    GROUP BY ClassId, ExamId
                );");

            migrationBuilder.CreateIndex(
                name: "IX_ClassAssignments_ClassId_ExamId",
                table: "ClassAssignments",
                columns: new[] { "ClassId", "ExamId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClassAssignments_ClassId_ExamId",
                table: "ClassAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_ClassAssignments_ClassId",
                table: "ClassAssignments",
                column: "ClassId");
        }
    }
}
