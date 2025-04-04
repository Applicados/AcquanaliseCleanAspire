using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanAspire.Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelForPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image",
                table: "products");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "products");

            migrationBuilder.DropColumn(
                name: "avatar",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "sku",
                table: "products",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "time_zone_id",
                table: "AspNetUsers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tenant_id",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "refresh_token",
                table: "AspNetUsers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nickname",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "last_modified_by",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "language_code",
                table: "AspNetUsers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "avatar_url",
                table: "AspNetUsers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "stocks",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    product_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    created = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stocks", x => x.id);
                    table.ForeignKey(
                        name: "fk_stocks_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stocks_product_id",
                table: "stocks",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_name",
                table: "tenants",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stocks");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropColumn(
                name: "category",
                table: "products");

            migrationBuilder.DropColumn(
                name: "sku",
                table: "products");

            migrationBuilder.DropColumn(
                name: "avatar_url",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<byte[]>(
                name: "image",
                table: "products",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "time_zone_id",
                table: "AspNetUsers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tenant_id",
                table: "AspNetUsers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "refresh_token",
                table: "AspNetUsers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                table: "AspNetUsers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nickname",
                table: "AspNetUsers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "last_modified_by",
                table: "AspNetUsers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "language_code",
                table: "AspNetUsers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "AspNetUsers",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "avatar",
                table: "AspNetUsers",
                type: "bytea",
                nullable: true);
        }
    }
}
