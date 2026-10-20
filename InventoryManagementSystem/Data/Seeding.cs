using InventoryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Data;

namespace InventoryManagementSystem.Data
{
    public static class Seeding
    {
        public static void SeedDatabase(ModelBuilder modelBuilder)
        {
            // Compile time model bindings
        }

        public static async Task EnsureSchemaAsync(AppDbContext context)
        {
            try
            {
                if (context.Database.IsSqlServer())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Projects')
                        BEGIN
                            CREATE TABLE Projects (
                                ProjectId INT IDENTITY(1,1) PRIMARY KEY,
                                Name NVARCHAR(250) NOT NULL DEFAULT '',
                                Code NVARCHAR(50) NOT NULL DEFAULT '',
                                Location NVARCHAR(500) NULL DEFAULT '',
                                Budget FLOAT NOT NULL DEFAULT 0,
                                Status NVARCHAR(50) NOT NULL DEFAULT 'Active',
                                SupervisorId INT NULL,
                                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                            );
                        END

                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                        BEGIN
                            CREATE TABLE Users (
                                UserId INT IDENTITY(1,1) PRIMARY KEY,
                                Username NVARCHAR(100) NOT NULL DEFAULT '',
                                Email NVARCHAR(200) NOT NULL DEFAULT '',
                                Password NVARCHAR(200) NOT NULL DEFAULT '',
                                FullName NVARCHAR(200) NOT NULL DEFAULT '',
                                Phone NVARCHAR(50) NULL DEFAULT '',
                                Role NVARCHAR(50) NOT NULL DEFAULT 'Supervisor',
                                AssignedProjectId INT NULL,
                                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                            );
                        END

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Categories') AND name = 'ProjectId')
                        BEGIN
                            ALTER TABLE Categories ADD ProjectId INT NULL;
                        END

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'ProjectId')
                        BEGIN
                            ALTER TABLE Suppliers ADD ProjectId INT NULL;
                        END

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ProjectId')
                        BEGIN
                            ALTER TABLE Products ADD ProjectId INT NULL;
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'Unit')
                        BEGIN
                            ALTER TABLE Products ADD Unit NVARCHAR(50) NOT NULL DEFAULT 'Units';
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'ReceiptSlipUrl')
                        BEGIN
                            ALTER TABLE Products ADD ReceiptSlipUrl NVARCHAR(MAX) NULL DEFAULT '';
                        END
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CreatedAt')
                        BEGIN
                            ALTER TABLE Products ADD CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE();
                        END

                        UPDATE Products SET Unit = 'Units' WHERE Unit IS NULL;
                        UPDATE Products SET ReceiptSlipUrl = '' WHERE ReceiptSlipUrl IS NULL;
                        UPDATE Products SET Description = '' WHERE Description IS NULL;
                        UPDATE Products SET Name = 'Material ' + CAST(ProductId AS VARCHAR) WHERE Name IS NULL;
                        UPDATE Products SET CreatedAt = GETUTCDATE() WHERE CreatedAt IS NULL;
                        UPDATE Categories SET Name = 'Category ' + CAST(CategoryId AS VARCHAR) WHERE Name IS NULL;
                        UPDATE Suppliers SET Name = 'Supplier ' + CAST(Id AS VARCHAR) WHERE Name IS NULL;
                        UPDATE Suppliers SET Address = '' WHERE Address IS NULL;
                        UPDATE Suppliers SET Phone = '' WHERE Phone IS NULL;
                    ");
                }
                else if (context.Database.IsNpgsql())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS ""Projects"" (
                            ""ProjectId"" SERIAL PRIMARY KEY,
                            ""Name"" TEXT NOT NULL DEFAULT '',
                            ""Code"" TEXT NOT NULL DEFAULT '',
                            ""Location"" TEXT NULL DEFAULT '',
                            ""Budget"" DOUBLE PRECISION NOT NULL DEFAULT 0,
                            ""Status"" TEXT NOT NULL DEFAULT 'Active',
                            ""SupervisorId"" INT NULL,
                            ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                        );

                        CREATE TABLE IF NOT EXISTS ""Users"" (
                            ""UserId"" SERIAL PRIMARY KEY,
                            ""Username"" TEXT NOT NULL DEFAULT '',
                            ""Email"" TEXT NOT NULL DEFAULT '',
                            ""Password"" TEXT NOT NULL DEFAULT '',
                            ""FullName"" TEXT NOT NULL DEFAULT '',
                            ""Phone"" TEXT NULL DEFAULT '',
                            ""Role"" TEXT NOT NULL DEFAULT 'Supervisor',
                            ""AssignedProjectId"" INT NULL,
                            ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
                        );

                        ALTER TABLE ""Categories"" ADD COLUMN IF NOT EXISTS ""ProjectId"" INT;
                        ALTER TABLE ""Suppliers"" ADD COLUMN IF NOT EXISTS ""ProjectId"" INT;
                        ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""ProjectId"" INT;
                        ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Unit"" TEXT DEFAULT 'Units';
                        ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""ReceiptSlipUrl"" TEXT DEFAULT '';
                        ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW();
                    ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Schema notice: {ex.Message}");
            }
        }

        public static async Task ResetToPakistaniConstructionDataAsync(AppDbContext context)
        {
            await EnsureSchemaAsync(context);

            try
            {
                var conn = context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                using var cmd = conn.CreateCommand();

                if (context.Database.IsSqlServer())
                {
                    cmd.CommandText = @"
                        -- 1. Clear old data
                        DELETE FROM Products;
                        DELETE FROM Categories;
                        DELETE FROM Suppliers;
                        DELETE FROM Users;
                        DELETE FROM Projects;

                        -- 2. Insert Projects
                        IF OBJECT_ID('Projects') IS NOT NULL
                        BEGIN
                            IF COLUMNPROPERTY(OBJECT_ID('Projects'), 'ProjectId', 'IsIdentity') = 1
                                SET IDENTITY_INSERT Projects ON;

                            INSERT INTO Projects (ProjectId, Name, Code, Location, Budget, Status, SupervisorId, CreatedAt)
                            VALUES 
                            (1, 'Gulshan Executive Heights (Karachi)', 'GEH-101', 'Plot 14-C, Block 13D, Main University Road, Gulshan-e-Iqbal, Karachi', 85000000, 'Active', NULL, GETUTCDATE()),
                            (2, 'DHA Phase 8 Luxury Villa Project', 'DHA-802', 'Street 22, Sector B, Phase 8, DHA Karachi', 48000000, 'Active', NULL, GETUTCDATE()),
                            (3, 'Bahria Town Commercial Complex', 'BTC-305', 'Commercial Precinct 10A, Bahria Town Karachi', 120000000, 'Active', NULL, GETUTCDATE()),
                            (4, 'Gulberg Greens Corporate Center (Islamabad)', 'GGC-401', 'Civic Center Commercial Area, Gulberg Greens, Islamabad', 95000000, 'Active', NULL, GETUTCDATE());

                            IF COLUMNPROPERTY(OBJECT_ID('Projects'), 'ProjectId', 'IsIdentity') = 1
                                SET IDENTITY_INSERT Projects OFF;
                        END

                        -- 3. Insert Users
                        IF OBJECT_ID('Users') IS NOT NULL
                        BEGIN
                            IF COLUMNPROPERTY(OBJECT_ID('Users'), 'UserId', 'IsIdentity') = 1
                                SET IDENTITY_INSERT Users ON;

                            INSERT INTO Users (UserId, Username, Email, Password, FullName, Phone, Role, AssignedProjectId, CreatedAt)
                            VALUES
                            (1, 'admin', 'admin@construction.com', 'admin123', 'Haji Abdul Rehman (Managing Director & Owner)', '+92-300-8245671', 'Admin', NULL, GETUTCDATE()),
                            (2, 'farhan_sup', 'farhan@construction.com', 'pass123', 'Engr. Farhan Siddiqui (Project Engineer)', '+92-321-9988771', 'Supervisor', 1, GETUTCDATE()),
                            (3, 'tariq_sup', 'tariq@construction.com', 'pass123', 'Site Incharge Tariq Mehmood', '+92-333-5544332', 'Supervisor', 2, GETUTCDATE()),
                            (4, 'hamza_sup', 'hamza@construction.com', 'pass123', 'Supervisor Hamza Khan', '+92-345-1122334', 'Supervisor', NULL, GETUTCDATE());

                            IF COLUMNPROPERTY(OBJECT_ID('Users'), 'UserId', 'IsIdentity') = 1
                                SET IDENTITY_INSERT Users OFF;
                        END

                        -- Link Supervisors to Projects
                        UPDATE Projects SET SupervisorId = 2 WHERE ProjectId = 1;
                        UPDATE Projects SET SupervisorId = 3 WHERE ProjectId = 2;

                        -- 4. Insert Categories
                        IF OBJECT_ID('Categories') IS NOT NULL
                        BEGIN
                            IF COLUMNPROPERTY(OBJECT_ID('Categories'), 'CategoryId', 'IsIdentity') = 1
                                SET IDENTITY_INSERT Categories ON;

                            INSERT INTO Categories (CategoryId, Name, ProjectId)
                            VALUES
                            (1, 'Cement & Concrete Mix', 1),
                            (2, 'Steel & Structural Rebars', 1),
                            (3, 'Bricks, Hollow Blocks & Masonry', 1),
                            (4, 'Plumbing, PVC & Sanitary Piping', 1),
                            (5, 'Electrical Conduits & Copper Cables', 1),
                            (6, 'Aggregates, Crush & River Sand', 1),
                            (7, 'Premium Porcelain Tiles & Marbles', 2),
                            (8, 'Sanitaryware & Concealed Bath Fittings', 2),
                            (9, 'Paints, Primers & Waterproofing', 2),
                            (10, 'Burma Teak Woodwork & Door Frames', 2),
                            (11, 'White Cement, Plastering & Grouting', 2);

                            IF COLUMNPROPERTY(OBJECT_ID('Categories'), 'CategoryId', 'IsIdentity') = 1
                                SET IDENTITY_INSERT Categories OFF;
                        END

                        -- 5. Insert Suppliers
                        IF OBJECT_ID('Suppliers') IS NOT NULL
                        BEGIN
                            IF COLUMNPROPERTY(OBJECT_ID('Suppliers'), 'Id', 'IsIdentity') = 1
                                SET IDENTITY_INSERT Suppliers ON;

                            INSERT INTO Suppliers (Id, Name, Address, Phone, ProjectId)
                            VALUES
                            (1, 'Lucky Cement Limited', 'Main Superhighway Industrial Logistics Hub, Karachi', '+92-21-34538901', 1),
                            (2, 'Amreli Steels Limited', 'D-89, SITE Industrial Area, Karachi', '+92-21-111-267-354', 1),
                            (3, 'Popular Pipes & Plastics', 'Plot 45, Sector 15, Korangi Industrial Area, Karachi', '+92-21-35061234', 1),
                            (4, 'Pakistan Cables Limited', 'B-21, Sindh Industrial Trading Estate (SITE), Karachi', '+92-21-111-222-537', 1),
                            (5, 'Al-Madina Aggregate & River Sand Depot', 'Hub River Road / Superhighway Depot, Karachi', '+92-300-2134567', 1),
                            (6, 'Master Tiles & Ceramics Industries', 'DHA Phase 2 Commercial Display Center, Karachi', '+92-42-111-627-837', 2),
                            (7, 'Porta Sanitaryware Pakistan', 'Main Sunset Blvd, Phase 2, DHA Karachi', '+92-21-35894321', 2),
                            (8, 'Berger Paints Pakistan Limited', 'Plot 36, Sector 15, Korangi Industrial Area, Karachi', '+92-21-35071289', 2),
                            (9, 'Fauji Cement Company Limited', 'Port Qasim Terminal Hub, Karachi', '+92-51-111-328-542', 2),
                            (10, 'Timber Market Traders (Teak Wood)', 'Main Timber Market, Old City Area, Karachi', '+92-21-32724455', 2);

                            IF COLUMNPROPERTY(OBJECT_ID('Suppliers'), 'Id', 'IsIdentity') = 1
                                SET IDENTITY_INSERT Suppliers OFF;
                        END

                        -- 6. Insert Products
                        IF OBJECT_ID('Products') IS NOT NULL
                        BEGIN
                            IF COLUMNPROPERTY(OBJECT_ID('Products'), 'ProductId', 'IsIdentity') = 1
                                SET IDENTITY_INSERT Products ON;

                            INSERT INTO Products (ProductId, Name, Description, Price, Quantity, Unit, ReceiptSlipUrl, CategoryId, SupplierId, ProjectId, CreatedAt)
                            VALUES
                            (1, 'Lucky Portland Cement Grade 53 (50kg Bags)', 'Ordinary Portland Cement (OPC Grade 53) for RCC column casting', 1450, 500, 'Bags', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 1, 1, 1, GETUTCDATE()),
                            (2, 'Amreli Grade-60 Deformed Steel Rebar (16mm)', 'High strength Grade 60 steel rebars for basement raft foundation', 265000, 24, 'Tons', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_2.jpg', 2, 2, 1, GETUTCDATE()),
                            (3, 'Amreli Grade-60 Deformed Steel Rebar (12mm)', '12mm yield strength steel rebars for beam reinforcements and floor slabs', 265000, 18, 'Tons', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_2.jpg', 2, 2, 1, GETUTCDATE()),
                            (4, 'Amreli Grade-60 Deformed Steel Rebar (10mm)', '10mm steel rebars for ring beam ties and column stirrups', 268000, 12, 'Tons', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_2.jpg', 2, 2, 1, GETUTCDATE()),
                            (5, 'Black Annealed Steel Binding Wire (18-Gauge)', 'Flexible soft annealed iron binding wire coil for fixing steel reinforcement', 320, 250, 'Kg', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_2.jpg', 2, 2, 1, GETUTCDATE()),
                            (6, 'A-Class Burnt Red Clay Kiln Bricks (9x4.5x3 in)', 'First-class machine-made red clay burnt bricks for exterior boundary walls', 14.5, 45000, 'Pieces', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 3, 5, 1, GETUTCDATE()),
                            (7, 'Chenab River Clean Washed Coarse Sand', 'Silt-free washed coarse river sand dumper load for plastering and concrete mix', 42000, 12, 'Dumper Trucks', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 6, 5, 1, GETUTCDATE()),
                            (8, 'Margalla Crushed Stone Aggregate (1/2 inch)', 'High strength machine crushed stone aggregate for RCC column casting', 58000, 15, 'Dumper Trucks', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 6, 5, 1, GETUTCDATE()),
                            (9, 'Popular 4-inch PVC Class-B Drainage Pipes (10ft)', 'Heavy duty Class-B PVC drainage and rainwater disposal pipes', 2850, 120, 'Lengths', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_3.jpg', 4, 3, 1, GETUTCDATE()),
                            (10, 'Master Class-A PPRC Water Pipe (25mm / 10ft)', 'Pressure PN-20 PPRC pipe for concealed hot and cold plumbing line installations', 980, 85, 'Lengths', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_3.jpg', 4, 3, 1, GETUTCDATE()),
                            (11, 'Pakistan Cables 7/029 Single Core Copper Cable (90m)', 'Pure copper 7/029 450/750V PVC insulated conduit cable coil for AC circuits', 18500, 40, 'Coils', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_3.jpg', 5, 4, 1, GETUTCDATE()),
                            (12, 'Pakistan Cables 3/029 Single Core Copper Cable (90m)', '99.9% pure copper 3/029 general lighting and fan circuit wire', 9200, 60, 'Coils', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_3.jpg', 5, 4, 1, GETUTCDATE()),
                            (13, 'Master 60x60cm Glazed Porcelain Floor Tiles (Carara White)', 'High gloss rectified porcelain floor tiles for lounge and master bedrooms', 2800, 420, 'Sq Meters', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 7, 6, 2, GETUTCDATE()),
                            (14, 'Ziarat White Premium Marble Slabs (12x24 inch)', 'First choice fine grade Ziarat White polished marble tiles for staircase treads', 240, 1200, 'Sq Ft', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 7, 6, 2, GETUTCDATE()),
                            (15, 'Porta Concealed Wall-Hung Cistern & Commode Set', 'Luxury concealed pneumatic tank with matte black flush actuator plate', 38500, 8, 'Sets', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_2.jpg', 8, 7, 2, GETUTCDATE()),
                            (16, 'Porta Luxury Single-Lever Brass Basin Mixers', 'Solid forged brass chrome plated vanity sink single-hole water mixer taps', 8200, 12, 'Pieces', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_2.jpg', 8, 7, 2, GETUTCDATE()),
                            (17, 'Berger Robbialac Weathercoat Exterior Emulsion (16L)', 'Silicon enriched high-durability all-weather exterior wall protective paint', 14800, 20, 'Drums', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_3.jpg', 9, 8, 2, GETUTCDATE()),
                            (18, 'SikaTop Seal-107 Waterproofing Chemical Pack (25kg)', 'Polymer modified cementitious waterproofing slurry for basement and water tanks', 8500, 25, 'Packs', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_3.jpg', 9, 8, 2, GETUTCDATE()),
                            (19, 'Hand-Crafted Solid Burma Teak Double Entrance Door (4x8 ft)', 'Seasoned Burma Teak wood double-leaf main entrance door with brass mortise lock', 95000, 2, 'Pieces', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 10, 10, 2, GETUTCDATE()),
                            (20, 'Commercial Grade BWP Plywood Sheets (8x4 ft, 18mm)', 'Boiling water proof (BWP) 18mm hardwood core plywood sheets for custom wardrobes', 6200, 45, 'Sheets', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 10, 10, 2, GETUTCDATE()),
                            (21, 'Fauji Super White Cement (40kg Bag)', 'Super white Portland cement for marble polishing and tile joint grouting', 2100, 40, 'Bags', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 11, 9, 2, GETUTCDATE());

                            IF COLUMNPROPERTY(OBJECT_ID('Products'), 'ProductId', 'IsIdentity') = 1
                                SET IDENTITY_INSERT Products OFF;
                        END
                    ";
                }
                else
                {
                    cmd.CommandText = @"
                        DELETE FROM ""Products"";
                        DELETE FROM ""Categories"";
                        DELETE FROM ""Suppliers"";
                        DELETE FROM ""Users"";
                        DELETE FROM ""Projects"";

                        INSERT INTO ""Projects"" (""ProjectId"", ""Name"", ""Code"", ""Location"", ""Budget"", ""Status"", ""SupervisorId"", ""CreatedAt"")
                        VALUES 
                        (1, 'Gulshan Executive Heights (Karachi)', 'GEH-101', 'Plot 14-C, Block 13D, Main University Road, Gulshan-e-Iqbal, Karachi', 85000000, 'Active', NULL, NOW()),
                        (2, 'DHA Phase 8 Luxury Villa Project', 'DHA-802', 'Street 22, Sector B, Phase 8, DHA Karachi', 48000000, 'Active', NULL, NOW()),
                        (3, 'Bahria Town Commercial Complex', 'BTC-305', 'Commercial Precinct 10A, Bahria Town Karachi', 120000000, 'Active', NULL, NOW()),
                        (4, 'Gulberg Greens Corporate Center (Islamabad)', 'GGC-401', 'Civic Center Commercial Area, Gulberg Greens, Islamabad', 95000000, 'Active', NULL, NOW())
                        ON CONFLICT DO NOTHING;

                        INSERT INTO ""Users"" (""UserId"", ""Username"", ""Email"", ""Password"", ""FullName"", ""Phone"", ""Role"", ""AssignedProjectId"", ""CreatedAt"")
                        VALUES
                        (1, 'admin', 'admin@construction.com', 'admin123', 'Haji Abdul Rehman (Managing Director & Owner)', '+92-300-8245671', 'Admin', NULL, NOW()),
                        (2, 'farhan_sup', 'farhan@construction.com', 'pass123', 'Engr. Farhan Siddiqui (Project Engineer)', '+92-321-9988771', 'Supervisor', 1, NOW()),
                        (3, 'tariq_sup', 'tariq@construction.com', 'pass123', 'Site Incharge Tariq Mehmood', '+92-333-5544332', 'Supervisor', 2, NOW()),
                        (4, 'hamza_sup', 'hamza@construction.com', 'pass123', 'Supervisor Hamza Khan', '+92-345-1122334', 'Supervisor', NULL, NOW())
                        ON CONFLICT DO NOTHING;

                        UPDATE ""Projects"" SET ""SupervisorId"" = 2 WHERE ""ProjectId"" = 1;
                        UPDATE ""Projects"" SET ""SupervisorId"" = 3 WHERE ""ProjectId"" = 2;

                        INSERT INTO ""Categories"" (""CategoryId"", ""Name"", ""ProjectId"")
                        VALUES
                        (1, 'Cement & Concrete Mix', 1),
                        (2, 'Steel & Structural Rebars', 1),
                        (3, 'Bricks, Hollow Blocks & Masonry', 1),
                        (4, 'Plumbing, PVC & Sanitary Piping', 1),
                        (5, 'Electrical Conduits & Copper Cables', 1),
                        (6, 'Aggregates, Crush & River Sand', 1),
                        (7, 'Premium Porcelain Tiles & Marbles', 2),
                        (8, 'Sanitaryware & Concealed Bath Fittings', 2),
                        (9, 'Paints, Primers & Waterproofing', 2),
                        (10, 'Burma Teak Woodwork & Door Frames', 2),
                        (11, 'White Cement, Plastering & Grouting', 2)
                        ON CONFLICT DO NOTHING;

                        INSERT INTO ""Suppliers"" (""Id"", ""Name"", ""Address"", ""Phone"", ""ProjectId"")
                        VALUES
                        (1, 'Lucky Cement Limited', 'Main Superhighway Industrial Logistics Hub, Karachi', '+92-21-34538901', 1),
                        (2, 'Amreli Steels Limited', 'D-89, SITE Industrial Area, Karachi', '+92-21-111-267-354', 1),
                        (3, 'Popular Pipes & Plastics', 'Plot 45, Sector 15, Korangi Industrial Area, Karachi', '+92-21-35061234', 1),
                        (4, 'Pakistan Cables Limited', 'B-21, Sindh Industrial Trading Estate (SITE), Karachi', '+92-21-111-222-537', 1),
                        (5, 'Al-Madina Aggregate & River Sand Depot', 'Hub River Road / Superhighway Depot, Karachi', '+92-300-2134567', 1),
                        (6, 'Master Tiles & Ceramics Industries', 'DHA Phase 2 Commercial Display Center, Karachi', '+92-42-111-627-837', 2),
                        (7, 'Porta Sanitaryware Pakistan', 'Main Sunset Blvd, Phase 2, DHA Karachi', '+92-21-35894321', 2),
                        (8, 'Berger Paints Pakistan Limited', 'Plot 36, Sector 15, Korangi Industrial Area, Karachi', '+92-21-35071289', 2),
                        (9, 'Fauji Cement Company Limited', 'Port Qasim Terminal Hub, Karachi', '+92-51-111-328-542', 2),
                        (10, 'Timber Market Traders (Teak Wood)', 'Main Timber Market, Old City Area, Karachi', '+92-21-32724455', 2)
                        ON CONFLICT DO NOTHING;

                        INSERT INTO ""Products"" (""ProductId"", ""Name"", ""Description"", ""Price"", ""Quantity"", ""Unit"", ""ReceiptSlipUrl"", ""CategoryId"", ""SupplierId"", ""ProjectId"", ""CreatedAt"")
                        VALUES
                        (1, 'Lucky Portland Cement Grade 53 (50kg Bags)', 'Ordinary Portland Cement (OPC Grade 53) for RCC column casting', 1450, 500, 'Bags', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 1, 1, 1, NOW()),
                        (2, 'Amreli Grade-60 Deformed Steel Rebar (16mm)', 'High strength Grade 60 steel rebars for basement raft foundation', 265000, 24, 'Tons', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_2.jpg', 2, 2, 1, NOW()),
                        (3, 'Amreli Grade-60 Deformed Steel Rebar (12mm)', '12mm yield strength steel rebars for beam reinforcements and floor slabs', 265000, 18, 'Tons', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_2.jpg', 2, 2, 1, NOW()),
                        (4, 'Amreli Grade-60 Deformed Steel Rebar (10mm)', '10mm steel rebars for ring beam ties and column stirrups', 268000, 12, 'Tons', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_2.jpg', 2, 2, 1, NOW()),
                        (5, 'Black Annealed Steel Binding Wire (18-Gauge)', 'Flexible soft annealed iron binding wire coil for fixing steel reinforcement', 320, 250, 'Kg', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_2.jpg', 2, 2, 1, NOW()),
                        (6, 'A-Class Burnt Red Clay Kiln Bricks (9x4.5x3 in)', 'First-class machine-made red clay burnt bricks for exterior boundary walls', 14.5, 45000, 'Pieces', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 3, 5, 1, NOW()),
                        (7, 'Chenab River Clean Washed Coarse Sand', 'Silt-free washed coarse river sand dumper load for plastering and concrete mix', 42000, 12, 'Dumper Trucks', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 6, 5, 1, NOW()),
                        (8, 'Margalla Crushed Stone Aggregate (1/2 inch)', 'High strength machine crushed stone aggregate for RCC column casting', 58000, 15, 'Dumper Trucks', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 6, 5, 1, NOW()),
                        (9, 'Popular 4-inch PVC Class-B Drainage Pipes (10ft)', 'Heavy duty Class-B PVC drainage and rainwater disposal pipes', 2850, 120, 'Lengths', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_3.jpg', 4, 3, 1, NOW()),
                        (10, 'Master Class-A PPRC Water Pipe (25mm / 10ft)', 'Pressure PN-20 PPRC pipe for concealed hot and cold plumbing line installations', 980, 85, 'Lengths', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_3.jpg', 4, 3, 1, NOW()),
                        (11, 'Pakistan Cables 7/029 Single Core Copper Cable (90m)', 'Pure copper 7/029 450/750V PVC insulated conduit cable coil for AC circuits', 18500, 40, 'Coils', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_3.jpg', 5, 4, 1, NOW()),
                        (12, 'Pakistan Cables 3/029 Single Core Copper Cable (90m)', '99.9% pure copper 3/029 general lighting and fan circuit wire', 9200, 60, 'Coils', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_3.jpg', 5, 4, 1, NOW()),
                        (13, 'Master 60x60cm Glazed Porcelain Floor Tiles (Carara White)', 'High gloss rectified porcelain floor tiles for lounge and master bedrooms', 2800, 420, 'Sq Meters', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 7, 6, 2, NOW()),
                        (14, 'Ziarat White Premium Marble Slabs (12x24 inch)', 'First choice fine grade Ziarat White polished marble tiles for staircase treads', 240, 1200, 'Sq Ft', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 7, 6, 2, NOW()),
                        (15, 'Porta Concealed Wall-Hung Cistern & Commode Set', 'Luxury concealed pneumatic tank with matte black flush actuator plate', 38500, 8, 'Sets', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_2.jpg', 8, 7, 2, NOW()),
                        (16, 'Porta Luxury Single-Lever Brass Basin Mixers', 'Solid forged brass chrome plated vanity sink single-hole water mixer taps', 8200, 12, 'Pieces', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_2.jpg', 8, 7, 2, NOW()),
                        (17, 'Berger Robbialac Weathercoat Exterior Emulsion (16L)', 'Silicon enriched high-durability all-weather exterior wall protective paint', 14800, 20, 'Drums', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_3.jpg', 9, 8, 2, NOW()),
                        (18, 'SikaTop Seal-107 Waterproofing Chemical Pack (25kg)', 'Polymer modified cementitious waterproofing slurry for basement and water tanks', 8500, 25, 'Packs', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_3.jpg', 9, 8, 2, NOW()),
                        (19, 'Hand-Crafted Solid Burma Teak Double Entrance Door (4x8 ft)', 'Seasoned Burma Teak wood double-leaf main entrance door with brass mortise lock', 95000, 2, 'Pieces', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 10, 10, 2, NOW()),
                        (20, 'Commercial Grade BWP Plywood Sheets (8x4 ft, 18mm)', 'Boiling water proof (BWP) 18mm hardwood core plywood sheets for custom wardrobes', 6200, 45, 'Sheets', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 10, 10, 2, NOW()),
                        (21, 'Fauji Super White Cement (40kg Bag)', 'Super white Portland cement for marble polishing and tile joint grouting', 2100, 40, 'Bags', 'https://res.cloudinary.com/diwj4lcwc/image/upload/v1725178900/samples/construction_slip_1.jpg', 11, 9, 2, NOW())
                        ON CONFLICT DO NOTHING;
                    ";
                }

                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine("Pakistani construction dataset populated successfully via direct SQL execution!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pakistani construction seed error: {ex.Message}");
            }
        }

        public static async Task SeedDataAsync(AppDbContext context)
        {
            await EnsureSchemaAsync(context);
            if (!context.Projects.Any() || !context.Products.Any(p => p.Name.Contains("Cement") || p.Name.Contains("Steel")))
            {
                await ResetToPakistaniConstructionDataAsync(context);
            }
        }
    }
}
