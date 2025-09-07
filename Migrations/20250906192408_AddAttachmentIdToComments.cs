using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_Requirements.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentIdToComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttachmentId",
                table: "Comments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AttachmentId",
                table: "Comments",
                column: "AttachmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_InventoryAttachments_AttachmentId",
                table: "Comments",
                column: "AttachmentId",
                principalTable: "InventoryAttachments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_InventoryAttachments_AttachmentId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_AttachmentId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "AttachmentId",
                table: "Comments");
        }
    }
}
