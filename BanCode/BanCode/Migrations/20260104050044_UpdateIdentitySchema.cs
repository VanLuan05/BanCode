using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanCode.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIdentitySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__categori__3213E83FB54B98FB", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    discount_percent = table.Column<int>(type: "int", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__coupons__3213E83FA95EA6D6", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "customer"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysdatetime())"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__users__3213E83F943E4598", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    short_description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tech_stack = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    thumbnail_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    demo_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    video_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "draft"),
                    is_featured = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__products__3213E83F3A547510", x => x.id);
                    table.ForeignKey(
                        name: "FK_products_category",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "pending"),
                    payment_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    transaction_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    coupon_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__orders__3213E83FE595B15B", x => x.id);
                    table.ForeignKey(
                        name: "FK_orders_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "download_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    product_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    downloaded_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysdatetime())"),
                    ip_address = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__download__3213E83FA54FCCF7", x => x.id);
                    table.ForeignKey(
                        name: "FK_download_product",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_download_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__product___3213E83FC27FCFAA", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_images_product",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    product_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    package_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    price = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    sale_price = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    features = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    file_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__product___3213E83F80226DBE", x => x.id);
                    table.ForeignKey(
                        name: "FK_packages_product",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    rating = table.Column<int>(type: "int", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__reviews__3213E83F92E1A53D", x => x.id);
                    table.ForeignKey(
                        name: "FK_reviews_product",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reviews_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    product_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    package_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    price_at_purchase = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__order_it__3213E83F7E1DEBC5", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_items_order",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_order_items_package",
                        column: x => x.package_id,
                        principalTable: "product_packages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_order_items_product",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "UQ__categori__32DD1E4C2A8DB217",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__coupons__357D4CF9D23DF631",
                table: "coupons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_download_logs_product_id",
                table: "download_logs",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_download_logs_user_id",
                table: "download_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_package_id",
                table: "order_items",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_product_id",
                table: "order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_user_id",
                table: "orders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_product_id",
                table: "product_images",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_packages_product_id",
                table: "product_packages",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "UQ__products__32DD1E4C527C641F",
                table: "products",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reviews_product_id",
                table: "reviews",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_user_id",
                table: "reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ__users__AB6E61640CFEAF2F",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coupons");

            migrationBuilder.DropTable(
                name: "download_logs");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "product_packages");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
