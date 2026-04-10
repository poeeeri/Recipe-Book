using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RecipeBook.Api.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dishes",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    calories = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    proteins = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    fats = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    carbs = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    portion_size = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    draft_calories = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    draft_proteins = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    draft_fats = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    draft_carbs = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dishes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    calories = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    proteins = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    fats = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    carbs = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    composition = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "text", nullable: false),
                    cooking_state = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dish_available_flags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dish_id = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dish_available_flags", x => x.id);
                    table.ForeignKey(
                        name: "FK_dish_available_flags_dishes_dish_id",
                        column: x => x.dish_id,
                        principalTable: "dishes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dish_flags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dish_id = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dish_flags", x => x.id);
                    table.ForeignKey(
                        name: "FK_dish_flags_dishes_dish_id",
                        column: x => x.dish_id,
                        principalTable: "dishes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dish_photos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dish_id = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dish_photos", x => x.id);
                    table.ForeignKey(
                        name: "FK_dish_photos_dishes_dish_id",
                        column: x => x.dish_id,
                        principalTable: "dishes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dish_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dish_id = table.Column<string>(type: "text", nullable: false),
                    product_id = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dish_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_dish_items_dishes_dish_id",
                        column: x => x.dish_id,
                        principalTable: "dishes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dish_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_flags",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_flags", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_flags_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_photos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_photos", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_photos_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dish_available_flags_dish_id_value",
                table: "dish_available_flags",
                columns: new[] { "dish_id", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dish_flags_dish_id_value",
                table: "dish_flags",
                columns: new[] { "dish_id", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dish_items_dish_id_product_id",
                table: "dish_items",
                columns: new[] { "dish_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dish_items_product_id",
                table: "dish_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_dish_photos_dish_id_sort_order",
                table: "dish_photos",
                columns: new[] { "dish_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_flags_product_id_value",
                table: "product_flags",
                columns: new[] { "product_id", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_photos_product_id_sort_order",
                table: "product_photos",
                columns: new[] { "product_id", "sort_order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dish_available_flags");

            migrationBuilder.DropTable(
                name: "dish_flags");

            migrationBuilder.DropTable(
                name: "dish_items");

            migrationBuilder.DropTable(
                name: "dish_photos");

            migrationBuilder.DropTable(
                name: "product_flags");

            migrationBuilder.DropTable(
                name: "product_photos");

            migrationBuilder.DropTable(
                name: "dishes");

            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
