using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OjisanBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderFulfillmentAndSingleOrderPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "Groups",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "OrderSubmissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "OrderSubmissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "OrderSubmissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingLabelUrl",
                table: "OrderSubmissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "OrderSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderSubmissionId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "Payments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSubmissions_ProductId",
                table: "OrderSubmissions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderSubmissionId",
                table: "Payments",
                column: "OrderSubmissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderSubmissions_Products_ProductId",
                table: "OrderSubmissions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_OrderSubmissions_OrderSubmissionId",
                table: "Payments",
                column: "OrderSubmissionId",
                principalTable: "OrderSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderSubmissions_Products_ProductId",
                table: "OrderSubmissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_OrderSubmissions_OrderSubmissionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_OrderSubmissions_ProductId",
                table: "OrderSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OrderSubmissionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "OrderSubmissions");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "OrderSubmissions");

            migrationBuilder.DropColumn(
                name: "TrackingNumber",
                table: "OrderSubmissions");

            migrationBuilder.DropColumn(
                name: "ShippingLabelUrl",
                table: "OrderSubmissions");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "OrderSubmissions");

            migrationBuilder.DropColumn(
                name: "OrderSubmissionId",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "GroupId",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
