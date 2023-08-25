using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace newstrek.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "News",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    URL = table.Column<string>(type: "varchar(max)", nullable: true),
                    Date = table.Column<string>(type: "varchar(max)", nullable: true),
                    Title = table.Column<string>(type: "varchar(max)", nullable: true),
                    Article = table.Column<string>(type: "varchar(max)", nullable: false),
                    Category = table.Column<string>(type: "varchar(max)", nullable: true),
                    Tag = table.Column<string>(type: "varchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_News", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "News");
        }
    }
}
