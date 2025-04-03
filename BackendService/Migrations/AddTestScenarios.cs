using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BackendService.Migrations
{
    public partial class AddTestScenarios : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestScenarios",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestScenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioSteps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TestScenarioId = table.Column<int>(nullable: false),
                    Order = table.Column<int>(nullable: false),
                    HttpRequestId = table.Column<int>(nullable: true),
                    HttpResponseId = table.Column<int>(nullable: true),
                    Name = table.Column<string>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioSteps_Requests_HttpRequestId",
                        column: x => x.HttpRequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ScenarioSteps_Responses_HttpResponseId",
                        column: x => x.HttpResponseId,
                        principalTable: "Responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ScenarioSteps_TestScenarios_TestScenarioId",
                        column: x => x.TestScenarioId,
                        principalTable: "TestScenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioSteps_HttpRequestId",
                table: "ScenarioSteps",
                column: "HttpRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioSteps_HttpResponseId",
                table: "ScenarioSteps",
                column: "HttpResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioSteps_TestScenarioId",
                table: "ScenarioSteps",
                column: "TestScenarioId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScenarioSteps");

            migrationBuilder.DropTable(
                name: "TestScenarios");
        }
    }
}