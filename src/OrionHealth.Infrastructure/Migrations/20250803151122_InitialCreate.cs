using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrionHealth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ORIONHEALTH");

            migrationBuilder.CreateTable(
                name: "PATIENTS",
                schema: "ORIONHEALTH",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    MEDICAL_RECORD_NUMBER = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    FULL_NAME = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    DATE_OF_BIRTH = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PATIENTS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OBSERVATION_RESULTS",
                schema: "ORIONHEALTH",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    PATIENT_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    OBSERVATION_ID = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    OBSERVATION_TEXT = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    OBSERVATION_VALUE = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    UNITS = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    OBSERVATION_DATE_TIME = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    STATUS = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OBSERVATION_RESULTS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OBSERVATION_RESULTS_PATIENTS_PATIENT_ID",
                        column: x => x.PATIENT_ID,
                        principalSchema: "ORIONHEALTH",
                        principalTable: "PATIENTS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OBSERVATION_RESULTS_PATIENT_ID",
                schema: "ORIONHEALTH",
                table: "OBSERVATION_RESULTS",
                column: "PATIENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTS_MEDICAL_RECORD_NUMBER",
                schema: "ORIONHEALTH",
                table: "PATIENTS",
                column: "MEDICAL_RECORD_NUMBER",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OBSERVATION_RESULTS",
                schema: "ORIONHEALTH");

            migrationBuilder.DropTable(
                name: "PATIENTS",
                schema: "ORIONHEALTH");
        }
    }
}
