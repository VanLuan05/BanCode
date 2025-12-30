CREATE DATABASE WebProductStore;
GO
USE WebProductStore;
GO

CREATE TABLE users (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    email NVARCHAR(255) NOT NULL UNIQUE,
    full_name NVARCHAR(255) NOT NULL,
    role NVARCHAR(20) NOT NULL DEFAULT 'customer',
    created_at DATETIME2 DEFAULT SYSDATETIME(),

    CONSTRAINT CK_users_role CHECK (role IN ('admin', 'customer'))
);


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


CREATE TABLE orders (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    user_id UNIQUEIDENTIFIER NOT NULL,
    total_amount DECIMAL(12,2) NOT NULL,
    status NVARCHAR(20) DEFAULT 'pending',
    payment_method NVARCHAR(50),
    transaction_id NVARCHAR(255),
    paid_at DATETIME2,
	coupon_id INT NULL,
    CONSTRAINT FK_orders_user
        FOREIGN KEY (user_id) REFERENCES users(id),

    CONSTRAINT CK_orders_status
        CHECK (status IN ('pending', 'completed', 'cancelled'))
);

ALTER TABLE orders 
ADD created_at DATETIME2 DEFAULT SYSDATETIME();


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

CREATE TABLE coupons (
    id INT IDENTITY(1,1) PRIMARY KEY,
    code NVARCHAR(50) NOT NULL UNIQUE,
    discount_percent INT CHECK (discount_percent BETWEEN 1 AND 100),
    expiry_date DATETIME2,
    is_active BIT DEFAULT 1
);

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

    -- 1. Lấy giá thực tế tại thời điểm mua (ưu tiên sale_price)
    SELECT @CurrentPrice = ISNULL(sale_price, price)
    FROM product_packages 
    WHERE id = @PackageId AND product_id = @ProductId AND is_active = 1;

    IF @CurrentPrice IS NULL
    BEGIN
        RAISERROR('Sản phẩm hoặc gói không tồn tại hoặc đã bị ngừng kinh doanh.', 16, 1);
        RETURN;
    END

    -- 2. Bắt đầu Transaction
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Thêm vào bảng orders
        INSERT INTO orders (id, user_id, total_amount, status, payment_method, created_at)
        VALUES (@OrderId, @UserId, @CurrentPrice, 'pending', @PaymentMethod, SYSDATETIME());

        -- Thêm vào bảng order_items
        INSERT INTO order_items (order_id, product_id, package_id, price_at_purchase)
        VALUES (@OrderId, @ProductId, @PackageId, @CurrentPrice);

        COMMIT TRANSACTION;
        
        -- Trả về ID đơn hàng để Frontend xử lý hiển thị QR Code thanh toán
        SELECT @OrderId AS NewOrderId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;


CREATE PROCEDURE usp_CompleteOrder
    @OrderId UNIQUEIDENTIFIER,
    @TransactionId NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra đơn hàng có tồn tại và đang ở trạng thái pending không
    IF NOT EXISTS (SELECT 1 FROM orders WHERE id = @OrderId AND status = 'pending')
    BEGIN
        RAISERROR('Đơn hàng không tồn tại hoặc đã được xử lý trước đó.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Cập nhật trạng thái đơn hàng
        UPDATE orders
        SET status = 'completed',
            transaction_id = @TransactionId,
            paid_at = SYSDATETIME()
        WHERE id = @OrderId;

        -- Tại đây có thể thêm logic ghi log vào bảng download_logs nếu cần
        -- Hoặc gửi thông báo vào bảng notifications

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;