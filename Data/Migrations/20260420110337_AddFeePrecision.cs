using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudentEnrollmentSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFeePrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "Code", "CreditHours", "EligibleProgrammeCodes", "Fee", "Title" },
                values: new object[,]
                {
                    { 1, "CSC101", 0, "", 1000m, "Introduction to Programming" },
                    { 2, "CSC102", 0, "", 1200m, "Web Development Fundamentals" },
                    { 3, "CSC201", 0, "", 1100m, "Object-Oriented Programming" },
                    { 4, "CSC230", 0, "", 1300m, "Database Systems" },
                    { 5, "CSC240", 0, "", 1250m, "Computer Networks" },
                    { 6, "CSC245", 0, "", 1400m, "Cloud Fundamentals" },
                    { 7, "CSC310", 0, "", 1500m, "Data Structures and Algorithms" },
                    { 8, "AIS260", 0, "", 1350m, "Applied Business Analytics" },
                    { 9, "DAT250", 0, "", 1150m, "Data Visualization" },
                    { 10, "CLD270", 0, "", 1600m, "Cloud Infrastructure Services" },
                    { 11, "CYB220", 0, "", 1700m, "Cyber Security Fundamentals" },
                    { 12, "MOB230", 0, "", 1550m, "Mobile App Development" },
                    { 13, "UXD210", 0, "", 1450m, "User Experience Design" },
                    { 14, "MAT201", 0, "", 1800m, "Discrete Mathematics" },
                    { 15, "MAT210", 0, "", 1750m, "Applied Calculus" },
                    { 16, "STA210", 0, "", 1900m, "Business Statistics" },
                    { 17, "ENG150", 0, "", 2000m, "Academic Writing" },
                    { 18, "COM110", 0, "", 2100m, "Communication Skills" },
                    { 19, "HIS220", 0, "", 2200m, "Malaysian Civilisation" },
                    { 20, "LAW160", 0, "", 2300m, "Business Law" },
                    { 21, "ACC110", 0, "", 2400m, "Accounting Principles" },
                    { 22, "BUS205", 0, "", 2500m, "Entrepreneurship" },
                    { 23, "ECO120", 0, "", 2600m, "Microeconomics" },
                    { 24, "FIN215", 0, "", 2700m, "Personal Finance" },
                    { 25, "MKT225", 0, "", 2800m, "Digital Marketing" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 25);
        }
    }
}
