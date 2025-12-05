using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyMeals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DayNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Option = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyMeals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DailyMealId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Stars = table.Column<int>(type: "INTEGER", nullable: false),
                    DayNumber = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealRatings_DailyMeals_DailyMealId",
                        column: x => x.DailyMealId,
                        principalTable: "DailyMeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyMeals_Description",
                table: "DailyMeals",
                column: "Description");

            migrationBuilder.CreateIndex(
                name: "IX_MealRatings_DailyMealId",
                table: "MealRatings",
                column: "DailyMealId");

            migrationBuilder.CreateIndex(
                name: "IX_MealRatings_DayNumber_UserId",
                table: "MealRatings",
                columns: new[] { "DayNumber", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealRatings");

            migrationBuilder.DropTable(
                name: "DailyMeals");
        }
    }
}
