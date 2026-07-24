using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Asaie.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialSovereignSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "AgriRiskRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberStateCode = table.Column<string>(type: "text", nullable: false),
                    RegionName = table.Column<string>(type: "text", nullable: false),
                    HazardType = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(384)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgriRiskRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgriRiskRecords_MemberStateCode",
                table: "AgriRiskRecords",
                column: "MemberStateCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgriRiskRecords");
        }
    }
}
