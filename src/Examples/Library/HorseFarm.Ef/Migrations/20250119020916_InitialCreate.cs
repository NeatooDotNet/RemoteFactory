using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseFarm.Ef.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HorseFarm",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HorseFarm", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Cart",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                CartType = table.Column<int>(type: "INTEGER", nullable: false),
                NumberOfHorses = table.Column<int>(type: "INTEGER", nullable: false),
                HorseFarmId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Cart", x => x.Id);
                table.ForeignKey(
                    name: "FK_Cart_HorseFarm_HorseFarmId",
                    column: x => x.HorseFarmId,
                    principalTable: "HorseFarm",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Pasture",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                HorseFarmId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Pasture", x => x.Id);
                table.ForeignKey(
                    name: "FK_Pasture_HorseFarm_HorseFarmId",
                    column: x => x.HorseFarmId,
                    principalTable: "HorseFarm",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Horse",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Breed = table.Column<int>(type: "INTEGER", nullable: false),
                CartId = table.Column<int>(type: "INTEGER", nullable: true),
                PastureId = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Horse", x => x.Id);
                table.ForeignKey(
                    name: "FK_Horse_Cart_CartId",
                    column: x => x.CartId,
                    principalTable: "Cart",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Horse_Pasture_PastureId",
                    column: x => x.PastureId,
                    principalTable: "Pasture",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_Cart_HorseFarmId",
            table: "Cart",
            column: "HorseFarmId");

        migrationBuilder.CreateIndex(
            name: "IX_Horse_CartId",
            table: "Horse",
            column: "CartId");

        migrationBuilder.CreateIndex(
            name: "IX_Horse_PastureId",
            table: "Horse",
            column: "PastureId");

        migrationBuilder.CreateIndex(
            name: "IX_Pasture_HorseFarmId",
            table: "Pasture",
            column: "HorseFarmId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Horse");

        migrationBuilder.DropTable(
            name: "Cart");

        migrationBuilder.DropTable(
            name: "Pasture");

        migrationBuilder.DropTable(
            name: "HorseFarm");
    }
}
