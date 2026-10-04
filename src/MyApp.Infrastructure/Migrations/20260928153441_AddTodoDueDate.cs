using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddTodoDueDate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Todos",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Todos", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Todos");
    }
}
