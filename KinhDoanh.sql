CREATE DATABASE WebProductStore;
GO
USE WebProductStore;
GO

-- =============================================
-- 1. BẢNG USERS (TÍCH HỢP IDENTITY)
-- =============================================
CREATE TABLE users (
    -- Các cột mặc định của IdentityUser<Guid>
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserName NVARCHAR(256) NULL,
    NormalizedUserName NVARCHAR(256) NULL,
    Email NVARCHAR(256) NULL,
    NormalizedEmail NVARCHAR(256) NULL,
    EmailConfirmed BIT NOT NULL DEFAULT 0,
    PasswordHash NVARCHAR(MAX) NULL,
    SecurityStamp NVARCHAR(MAX) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL,
    PhoneNumber NVARCHAR(MAX) NULL,
    PhoneNumberConfirmed BIT NOT NULL DEFAULT 0,
    TwoFactorEnabled BIT NOT NULL DEFAULT 0,
    LockoutEnd DATETIMEOFFSET NULL,
    LockoutEnabled BIT NOT NULL DEFAULT 0,
    AccessFailedCount INT NOT NULL DEFAULT 0,

    -- Các cột tùy chỉnh của bạn (Custom Properties)
    full_name NVARCHAR(255) NOT NULL,
    role NVARCHAR(20) NOT NULL DEFAULT 'customer',
    created_at DATETIME2 DEFAULT SYSDATETIME(),

    CONSTRAINT CK_users_role CHECK (role IN ('admin', 'customer'))
);

-- Tạo Index tìm kiếm nhanh cho Identity
CREATE INDEX IX_users_NormalizedUserName ON users (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
CREATE INDEX IX_users_NormalizedEmail ON users (NormalizedEmail) WHERE NormalizedEmail IS NOT NULL;
GO

-- =============================================
-- 2. CÁC BẢNG HỆ THỐNG IDENTITY (ASP.NET CORE)
-- =============================================

-- Bảng Roles
CREATE TABLE AspNetRoles (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(256) NULL,
    NormalizedName NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL
);
CREATE INDEX IX_AspNetRoles_NormalizedName ON AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;

-- Bảng UserClaims
CREATE TABLE AspNetUserClaims (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AspNetUserClaims_users_UserId FOREIGN KEY (UserId) REFERENCES users (id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserClaims_UserId ON AspNetUserClaims (UserId);

-- Bảng UserLogins (Đã tối ưu độ dài khóa chính 128 ký tự)
CREATE TABLE AspNetUserLogins (
    LoginProvider NVARCHAR(128) NOT NULL,
    ProviderKey NVARCHAR(128) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX) NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_users_UserId FOREIGN KEY (UserId) REFERENCES users (id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserLogins_UserId ON AspNetUserLogins (UserId);

-- Bảng UserRoles
CREATE TABLE AspNetUserRoles (
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_users_UserId FOREIGN KEY (UserId) REFERENCES users (id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserRoles_RoleId ON AspNetUserRoles (RoleId);

-- Bảng UserTokens (Đã tối ưu độ dài khóa chính 128 ký tự)
CREATE TABLE AspNetUserTokens (
    UserId UNIQUEIDENTIFIER NOT NULL,
    LoginProvider NVARCHAR(128) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Value NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_users_UserId FOREIGN KEY (UserId) REFERENCES users (id) ON DELETE CASCADE
);

-- Bảng RoleClaims
CREATE TABLE AspNetRoleClaims (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetRoleClaims_RoleId ON AspNetRoleClaims (RoleId);
GO

-- =============================================
-- 3. CÁC BẢNG NGHIỆP VỤ BÁN HÀNG
-- =============================================

CREATE TABLE categories (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(255) NOT NULL,
    slug NVARCHAR(255) NOT NULL UNIQUE,
    description NVARCHAR(MAX)
);

CREATE TABLE products (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    category_id INT NOT NULL,
    title NVARCHAR(255) NOT NULL,
    slug NVARCHAR(255) NOT NULL UNIQUE,
    short_description NVARCHAR(500),
    content NVARCHAR(MAX),
    tech_stack NVARCHAR(MAX), -- JSON
    thumbnail_url NVARCHAR(500),
    demo_url NVARCHAR(500),
    video_url NVARCHAR(500),
    status NVARCHAR(20) DEFAULT 'draft',
    is_featured BIT DEFAULT 0,
    created_at DATETIME2 DEFAULT SYSDATETIME(),

    CONSTRAINT FK_products_category 
        FOREIGN KEY (category_id) REFERENCES categories(id),
    CONSTRAINT CK_products_status 
        CHECK (status IN ('draft', 'published')),
    CONSTRAINT CK_products_tech_stack 
        CHECK (ISJSON(tech_stack) > 0)
);

CREATE TABLE product_packages (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    product_id UNIQUEIDENTIFIER NOT NULL,
    package_type NVARCHAR(20) NOT NULL,
    price DECIMAL(12,2) NOT NULL,
    sale_price DECIMAL(12,2),
    features NVARCHAR(MAX), -- JSON
    file_url NVARCHAR(500),
    is_active BIT DEFAULT 1,

    CONSTRAINT FK_packages_product
        FOREIGN KEY (product_id) REFERENCES products(id),
    CONSTRAINT CK_package_type
        CHECK (package_type IN ('basic', 'pro', 'premium')),
    CONSTRAINT CK_packages_features 
        CHECK (ISJSON(features) > 0)
);

CREATE TABLE product_images (
    id INT IDENTITY(1,1) PRIMARY KEY,
    product_id UNIQUEIDENTIFIER NOT NULL,
    image_url NVARCHAR(500) NOT NULL,
    display_order INT DEFAULT 0,
    CONSTRAINT FK_product_images_product FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE TABLE coupons (
    id INT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(50) NOT NULL UNIQUE,
    discount_percent INT CHECK (discount_percent BETWEEN 1 AND 100),
    expiry_date DATETIME2,
    is_active BIT DEFAULT 1
);

CREATE TABLE orders (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    user_id UNIQUEIDENTIFIER NOT NULL,
    total_amount DECIMAL(12,2) NOT NULL,
    status NVARCHAR(20) DEFAULT 'pending',
    payment_method NVARCHAR(50),
    transaction_id NVARCHAR(255),
    paid_at DATETIME2,
    coupon_id INT NULL,
    created_at DATETIME2 DEFAULT SYSDATETIME(),

    CONSTRAINT FK_orders_user
        FOREIGN KEY (user_id) REFERENCES users(id),
    -- Cập nhật quy tắc trạng thái mới nhất (pending -> processing -> completed)
    CONSTRAINT CK_orders_status
        CHECK (status IN ('pending', 'processing', 'completed', 'cancelled'))
);

CREATE TABLE order_items (
    id INT IDENTITY(1,1) PRIMARY KEY,
    order_id UNIQUEIDENTIFIER NOT NULL,
    product_id UNIQUEIDENTIFIER NOT NULL,
    package_id UNIQUEIDENTIFIER NOT NULL,
    price_at_purchase DECIMAL(12,2) NOT NULL,

    CONSTRAINT FK_order_items_order
        FOREIGN KEY (order_id) REFERENCES orders(id),
    CONSTRAINT FK_order_items_product
        FOREIGN KEY (product_id) REFERENCES products(id),
    CONSTRAINT FK_order_items_package
        FOREIGN KEY (package_id) REFERENCES product_packages(id)
);

CREATE TABLE reviews (
    id INT IDENTITY(1,1) PRIMARY KEY,
    product_id UNIQUEIDENTIFIER NOT NULL,
    user_id UNIQUEIDENTIFIER NOT NULL,
    rating INT NOT NULL,
    comment NVARCHAR(MAX),
    created_at DATETIME2 DEFAULT SYSDATETIME(),

    CONSTRAINT FK_reviews_product
        FOREIGN KEY (product_id) REFERENCES products(id),
    CONSTRAINT FK_reviews_user
        FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT CK_reviews_rating
        CHECK (rating BETWEEN 1 AND 5)
);

CREATE TABLE download_logs (
    id INT IDENTITY(1,1) PRIMARY KEY,
    user_id UNIQUEIDENTIFIER NOT NULL,
    product_id UNIQUEIDENTIFIER NOT NULL,
    downloaded_at DATETIME2 DEFAULT SYSDATETIME(),
    ip_address NVARCHAR(45),
    CONSTRAINT FK_download_user FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT FK_download_product FOREIGN KEY (product_id) REFERENCES products(id)
);
GO

-- =============================================
-- 4. STORED PROCEDURES
-- =============================================

CREATE PROCEDURE usp_CreateOrder
    @UserId UNIQUEIDENTIFIER,
    @ProductId UNIQUEIDENTIFIER,
    @PackageId UNIQUEIDENTIFIER,
    @PaymentMethod NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CurrentPrice DECIMAL(12,2);
    DECLARE @OrderId UNIQUEIDENTIFIER = NEWID();

    -- Lấy giá thực tế (ưu tiên sale_price)
    SELECT @CurrentPrice = ISNULL(sale_price, price)
    FROM product_packages 
    WHERE id = @PackageId AND product_id = @ProductId AND is_active = 1;

    IF @CurrentPrice IS NULL
    BEGIN
        RAISERROR('Sản phẩm hoặc gói không tồn tại hoặc đã bị ngừng kinh doanh.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;
        INSERT INTO orders (id, user_id, total_amount, status, payment_method, created_at)
        VALUES (@OrderId, @UserId, @CurrentPrice, 'pending', @PaymentMethod, SYSDATETIME());

        INSERT INTO order_items (order_id, product_id, package_id, price_at_purchase)
        VALUES (@OrderId, @ProductId, @PackageId, @CurrentPrice);
        COMMIT TRANSACTION;
        
        SELECT @OrderId AS NewOrderId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

CREATE PROCEDURE usp_CompleteOrder
    @OrderId UNIQUEIDENTIFIER,
    @TransactionId NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM orders WHERE id = @OrderId AND status = 'pending')
    BEGIN
        RAISERROR('Đơn hàng không tồn tại hoặc đã được xử lý trước đó.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;
        UPDATE orders
        SET status = 'completed',
            transaction_id = @TransactionId,
            paid_at = SYSDATETIME()
        WHERE id = @OrderId;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

--==========================================================
USE WebProductStore;
UPDATE users SET role = 'admin' WHERE email = 'luannguyenqn.00@gmail.com';
select * from users
select * from orders