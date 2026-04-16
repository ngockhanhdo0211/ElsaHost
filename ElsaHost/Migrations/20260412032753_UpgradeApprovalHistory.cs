using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElsaHost.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeApprovalHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApproverId",
                table: "ApprovalHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApproverName",
                table: "ApprovalHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromStatus",
                table: "ApprovalHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromStep",
                table: "ApprovalHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToStatus",
                table: "ApprovalHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToStep",
                table: "ApprovalHistories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApproverId",
                table: "ApprovalHistories");

            migrationBuilder.DropColumn(
                name: "ApproverName",
                table: "ApprovalHistories");

            migrationBuilder.DropColumn(
                name: "FromStatus",
                table: "ApprovalHistories");

            migrationBuilder.DropColumn(
                name: "FromStep",
                table: "ApprovalHistories");

            migrationBuilder.DropColumn(
                name: "ToStatus",
                table: "ApprovalHistories");

            migrationBuilder.DropColumn(
                name: "ToStep",
                table: "ApprovalHistories");
        }
    }
}
