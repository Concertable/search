using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Concertable.Search.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "search");

            migrationBuilder.CreateTable(
                name: "ArtistRatingProjections",
                schema: "search",
                columns: table => new
                {
                    ArtistId = table.Column<int>(type: "int", nullable: false),
                    AverageRating = table.Column<double>(type: "float", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistRatingProjections", x => x.ArtistId);
                });

            migrationBuilder.CreateTable(
                name: "Artists",
                schema: "search",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<Point>(type: "geography", nullable: false),
                    County = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Town = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConcertRatingProjections",
                schema: "search",
                columns: table => new
                {
                    ConcertId = table.Column<int>(type: "int", nullable: false),
                    AverageRating = table.Column<double>(type: "float", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcertRatingProjections", x => x.ConcertId);
                });

            migrationBuilder.CreateTable(
                name: "Concerts",
                schema: "search",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ArtistId = table.Column<int>(type: "int", nullable: false),
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalTickets = table.Column<int>(type: "int", nullable: false),
                    AvailableTickets = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatePosted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Location = table.Column<Point>(type: "geography", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Concerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VenueRatingProjections",
                schema: "search",
                columns: table => new
                {
                    VenueId = table.Column<int>(type: "int", nullable: false),
                    AverageRating = table.Column<double>(type: "float", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueRatingProjections", x => x.VenueId);
                });

            migrationBuilder.CreateTable(
                name: "Venues",
                schema: "search",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<Point>(type: "geography", nullable: false),
                    County = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Town = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArtistGenres",
                schema: "search",
                columns: table => new
                {
                    ArtistId = table.Column<int>(type: "int", nullable: false),
                    Genre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistGenres", x => new { x.ArtistId, x.Genre });
                    table.ForeignKey(
                        name: "FK_ArtistGenres_Artists_ArtistId",
                        column: x => x.ArtistId,
                        principalSchema: "search",
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConcertGenres",
                schema: "search",
                columns: table => new
                {
                    ConcertId = table.Column<int>(type: "int", nullable: false),
                    Genre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConcertGenres", x => new { x.ConcertId, x.Genre });
                    table.ForeignKey(
                        name: "FK_ConcertGenres_Concerts_ConcertId",
                        column: x => x.ConcertId,
                        principalSchema: "search",
                        principalTable: "Concerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtistGenres",
                schema: "search");

            migrationBuilder.DropTable(
                name: "ArtistRatingProjections",
                schema: "search");

            migrationBuilder.DropTable(
                name: "ConcertGenres",
                schema: "search");

            migrationBuilder.DropTable(
                name: "ConcertRatingProjections",
                schema: "search");

            migrationBuilder.DropTable(
                name: "VenueRatingProjections",
                schema: "search");

            migrationBuilder.DropTable(
                name: "Venues",
                schema: "search");

            migrationBuilder.DropTable(
                name: "Artists",
                schema: "search");

            migrationBuilder.DropTable(
                name: "Concerts",
                schema: "search");
        }
    }
}
