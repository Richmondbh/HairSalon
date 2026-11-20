using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HairSalon.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyAndFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "Appointments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FavoriteStylists",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    StylistId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteStylists", x => new { x.UserId, x.StylistId });
                    table.ForeignKey(
                        name: "FK_FavoriteStylists_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavoriteStylists_Stylists_StylistId",
                        column: x => x.StylistId,
                        principalTable: "Stylists",
                        principalColumn: "StylistId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteStylists_StylistId",
                table: "FavoriteStylists",
                column: "StylistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteStylists");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Appointments");
        }
    }
}
