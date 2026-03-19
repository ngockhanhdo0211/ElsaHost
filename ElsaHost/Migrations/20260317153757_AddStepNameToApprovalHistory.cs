using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElsaHost.Migrations
{
    /// <inheritdoc />
    public partial class AddStepNameToApprovalHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StepName",
                table: "ApprovalHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StepName",
                table: "ApprovalHistories");
        }
    }
}
