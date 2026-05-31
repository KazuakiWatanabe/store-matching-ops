using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    detail = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_activities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    phone_hash = table.Column<string>(type: "text", nullable: true),
                    email_hash = table.Column<string>(type: "text", nullable: true),
                    visit_count = table.Column<int>(type: "integer", nullable: false),
                    last_visit_on = table.Column<DateOnly>(type: "date", nullable: true),
                    opt_in_status = table.Column<int>(type: "integer", nullable: false),
                    last_notified_on = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    discount_cap = table.Column<string>(type: "jsonb", nullable: true),
                    conditions = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "time_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    time_range = table.Column<string>(type: "jsonb", nullable: false),
                    supported_offer_categories = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_slots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_tenant_id",
                table: "audit_logs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_activities_customer_id",
                table: "customer_activities",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_activities_tenant_id",
                table: "customer_activities",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_customers_tenant_id",
                table: "customers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_offers_tenant_id",
                table: "offers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_resources_tenant_id",
                table: "resources",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_time_slots_resource_id",
                table: "time_slots",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_time_slots_tenant_id",
                table: "time_slots",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "customer_activities");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "offers");

            migrationBuilder.DropTable(
                name: "resources");

            migrationBuilder.DropTable(
                name: "time_slots");
        }
    }
}
