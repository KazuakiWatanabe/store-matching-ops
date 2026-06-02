using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExperimentsAndConversions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversion_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    revenue = table.Column<decimal>(type: "numeric", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversion_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "experiment_assignments",
                columns: table => new
                {
                    experiment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    arm = table.Column<string>(type: "text", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiment_assignments", x => new { x.experiment_id, x.customer_id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversion_events_campaign_id",
                table: "conversion_events",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "IX_conversion_events_tenant_id",
                table: "conversion_events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_experiment_assignments_campaign_id",
                table: "experiment_assignments",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "IX_experiment_assignments_tenant_id",
                table: "experiment_assignments",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversion_events");

            migrationBuilder.DropTable(
                name: "experiment_assignments");
        }
    }
}
