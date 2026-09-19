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

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "ArtistRatingProjections",
                schema: "search",
                columns: table => new
                {
                    ArtistId = table.Column<int>(type: "integer", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Avatar = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geography (point, 4326)", nullable: false),
                    County = table.Column<string>(type: "text", nullable: false),
                    Town = table.Column<string>(type: "text", nullable: false)
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
                    ConcertId = table.Column<int>(type: "integer", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ArtistId = table.Column<int>(type: "integer", nullable: false),
                    VenueId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalTickets = table.Column<int>(type: "integer", nullable: false),
                    AvailableTickets = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DatePosted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Location = table.Column<Point>(type: "geography (point, 4326)", nullable: false)
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
                    VenueId = table.Column<int>(type: "integer", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Avatar = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geography (point, 4326)", nullable: false),
                    County = table.Column<string>(type: "text", nullable: false),
                    Town = table.Column<string>(type: "text", nullable: false)
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
                    ArtistId = table.Column<int>(type: "integer", nullable: false),
                    Genre = table.Column<int>(type: "integer", nullable: false)
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
                    ConcertId = table.Column<int>(type: "integer", nullable: false),
                    Genre = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Artists_Location",
                schema: "search",
                table: "Artists",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_Concerts_Location",
                schema: "search",
                table: "Concerts",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_Venues_Location",
                schema: "search",
                table: "Venues",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");
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
