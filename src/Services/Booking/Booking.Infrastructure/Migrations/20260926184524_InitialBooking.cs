using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialBooking : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Bookings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EventId = table.Column<int>(type: "int", nullable: false),
                EventTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                EventStartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                OrganizerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                Total = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                PaymentRef = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SeatReleasePending = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bookings", x => x.Id);
                table.CheckConstraint("CK_Bookings_Pending", "[SeatReleasePending] = 0 OR [Status] = 'Cancelled'");
                table.CheckConstraint("CK_Bookings_Price", "[UnitPrice] >= 0 AND [Total] = [Quantity] * [UnitPrice]");
                table.CheckConstraint("CK_Bookings_Quantity", "[Quantity] BETWEEN 1 AND 10");
                table.CheckConstraint("CK_Bookings_Status", "[Status] IN ('Confirmed', 'Cancelled')");
            });

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_OrganizerId",
            table: "Bookings",
            column: "OrganizerId");

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_ReservationId",
            table: "Bookings",
            column: "ReservationId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Bookings_UserId_CreatedAt",
            table: "Bookings",
            columns: new[] { "UserId", "CreatedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Bookings");
    }
}
