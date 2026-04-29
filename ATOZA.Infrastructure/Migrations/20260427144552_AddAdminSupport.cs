using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATOZA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [UserName] = N'admin' OR [Email] = N'admin@atoza.vn')
                BEGIN
                    SET IDENTITY_INSERT [Users] ON;
                    INSERT INTO [Users] ([Id], [CreatedAt], [Email], [FullName], [IsActive], [PasswordHash], [Role], [UserName])
                    VALUES (-1, '2026-01-01T00:00:00.0000000Z', N'admin@atoza.vn', N'System Admin', CAST(1 AS bit), N'PBKDF2$100000$39Xdq+NQ2Yt9iv9N838zgw==$+JZYl/jpXSEclHFp1LCWzWwVSMI7gqNtQqX3045FutE=', 2, N'admin');
                    SET IDENTITY_INSERT [Users] OFF;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: -1);

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");
        }
    }
}
