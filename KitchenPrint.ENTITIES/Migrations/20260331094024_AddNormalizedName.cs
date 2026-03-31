using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitchenPrint.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Ingredients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_NormalizedName",
                table: "Ingredients",
                column: "NormalizedName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ingredients_NormalizedName",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Ingredients");
        }
    }
}
