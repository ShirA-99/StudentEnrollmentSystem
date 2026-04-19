using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentEnrollmentSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeparateRegistrationWindowsAndProgrammeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProgramCode",
                table: "StudentProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "AddDropEndDate",
                table: "Semesters",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "SemesterStartDate",
                table: "Semesters",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "EligibleProgrammeCodes",
                table: "Courses",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProgramCode",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "AddDropEndDate",
                table: "Semesters");

            migrationBuilder.DropColumn(
                name: "SemesterStartDate",
                table: "Semesters");

            migrationBuilder.DropColumn(
                name: "EligibleProgrammeCodes",
                table: "Courses");
        }
    }
}
