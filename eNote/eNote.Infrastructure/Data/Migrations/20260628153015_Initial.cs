using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eNote.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Address",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Street = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Address", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AspNetRoles",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "InstrumentType",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                MonthlyFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InstrumentType", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "InstrumentView",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(type: "integer", nullable: false),
                InstrumentId = table.Column<int>(type: "integer", nullable: false),
                ViewCount = table.Column<int>(type: "integer", nullable: false),
                LastViewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InstrumentView", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "MusicStore",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                StoreName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                BusinessHours = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MusicStore", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Notification",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(type: "integer", nullable: false),
                RentalId = table.Column<int>(type: "integer", nullable: true),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notification", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RentalNotificationOutbox",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                PayloadJson = table.Column<string>(type: "text", nullable: false),
                PublishedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                Attempts = table.Column<int>(type: "integer", nullable: false),
                LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RentalNotificationOutbox", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RevokedToken",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Jti = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                RevokedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RevokedToken", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUsers",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                FirstName = table.Column<string>(type: "text", nullable: true),
                LastName = table.Column<string>(type: "text", nullable: true),
                DateOfBirth = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                Picture = table.Column<byte[]>(type: "bytea", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                AddressId = table.Column<int>(type: "integer", nullable: true),
                UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: true),
                SecurityStamp = table.Column<string>(type: "text", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                PhoneNumber = table.Column<string>(type: "text", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetUsers_Address_AddressId",
                    column: x => x.AddressId,
                    principalTable: "Address",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AspNetRoleClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                RoleId = table.Column<int>(type: "integer", nullable: false),
                ClaimType = table.Column<string>(type: "text", nullable: true),
                ClaimValue = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Instrument",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                InstrumentTypeId = table.Column<int>(type: "integer", nullable: false),
                MusicStoreId = table.Column<int>(type: "integer", nullable: false),
                Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                ImagePath = table.Column<string>(type: "text", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Instrument", x => x.Id);
                table.ForeignKey(
                    name: "FK_Instrument_InstrumentType_InstrumentTypeId",
                    column: x => x.InstrumentTypeId,
                    principalTable: "InstrumentType",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Instrument_MusicStore_MusicStoreId",
                    column: x => x.MusicStoreId,
                    principalTable: "MusicStore",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(type: "integer", nullable: false),
                ClaimType = table.Column<string>(type: "text", nullable: true),
                ClaimValue = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "text", nullable: false),
                ProviderKey = table.Column<string>(type: "text", nullable: false),
                ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                UserId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserRoles",
            columns: table => new
            {
                UserId = table.Column<int>(type: "integer", nullable: false),
                RoleId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                UserId = table.Column<int>(type: "integer", nullable: false),
                LoginProvider = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Value = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Instructor",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                AppUserId = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Instructor", x => x.Id);
                table.ForeignKey(
                    name: "FK_Instructor_AspNetUsers_AppUserId",
                    column: x => x.AppUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MusicStoreEmployee",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                AppUserId = table.Column<int>(type: "integer", nullable: false),
                MusicStoreId = table.Column<int>(type: "integer", nullable: false),
                IsManager = table.Column<bool>(type: "boolean", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MusicStoreEmployee", x => x.Id);
                table.ForeignKey(
                    name: "FK_MusicStoreEmployee_AspNetUsers_AppUserId",
                    column: x => x.AppUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_MusicStoreEmployee_MusicStore_MusicStoreId",
                    column: x => x.MusicStoreId,
                    principalTable: "MusicStore",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Student",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                AppUserId = table.Column<int>(type: "integer", nullable: false),
                EnrollmentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                MembershipPaidUntil = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Student", x => x.Id);
                table.ForeignKey(
                    name: "FK_Student_AspNetUsers_AppUserId",
                    column: x => x.AppUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Course",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                InstructorId = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Course", x => x.Id);
                table.ForeignKey(
                    name: "FK_Course_Instructor_InstructorId",
                    column: x => x.InstructorId,
                    principalTable: "Instructor",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "InstrumentRental",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                StudentProfileId = table.Column<int>(type: "integer", nullable: false),
                InstrumentId = table.Column<int>(type: "integer", nullable: false),
                RentalStatus = table.Column<int>(type: "integer", nullable: false),
                RequestNote = table.Column<string>(type: "text", nullable: true),
                Note = table.Column<string>(type: "text", nullable: true),
                RequestedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                ApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                RejectedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                PickedUpAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                ReturnedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                ApprovedById = table.Column<int>(type: "integer", nullable: true),
                RejectedById = table.Column<int>(type: "integer", nullable: true),
                Fee = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InstrumentRental", x => x.Id);
                table.ForeignKey(
                    name: "FK_InstrumentRental_Instrument_InstrumentId",
                    column: x => x.InstrumentId,
                    principalTable: "Instrument",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_InstrumentRental_Student_StudentProfileId",
                    column: x => x.StudentProfileId,
                    principalTable: "Student",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Announcement",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CourseId = table.Column<int>(type: "integer", nullable: true),
                MusicStoreId = table.Column<int>(type: "integer", nullable: true),
                Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                ImagePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                PublishedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Announcement", x => x.Id);
                table.CheckConstraint("CK_Announcement_Scope", "(\"CourseId\" IS NOT NULL AND \"MusicStoreId\" IS NULL) OR (\"CourseId\" IS NULL AND \"MusicStoreId\" IS NOT NULL)");
                table.ForeignKey(
                    name: "FK_Announcement_AspNetUsers_CreatedById",
                    column: x => x.CreatedById,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Announcement_Course_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Course",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Announcement_MusicStore_MusicStoreId",
                    column: x => x.MusicStoreId,
                    principalTable: "MusicStore",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Enrollment",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                StudentId = table.Column<int>(type: "integer", nullable: false),
                CourseId = table.Column<int>(type: "integer", nullable: false),
                EnrollmentStatus = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Enrollment", x => x.Id);
                table.ForeignKey(
                    name: "FK_Enrollment_Course_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Course",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Enrollment_Student_StudentId",
                    column: x => x.StudentId,
                    principalTable: "Student",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Lecture",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CourseId = table.Column<int>(type: "integer", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                LectureType = table.Column<int>(type: "integer", nullable: false),
                LectureTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                Duration = table.Column<int>(type: "integer", nullable: false),
                Capacity = table.Column<int>(type: "integer", nullable: true),
                LectureStatus = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Lecture", x => x.Id);
                table.ForeignKey(
                    name: "FK_Lecture_Course_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Course",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Assignment",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                LectureId = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                DueAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Assignment", x => x.Id);
                table.ForeignKey(
                    name: "FK_Assignment_Lecture_LectureId",
                    column: x => x.LectureId,
                    principalTable: "Lecture",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Attendance",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                StudentId = table.Column<int>(type: "integer", nullable: false),
                LectureId = table.Column<int>(type: "integer", nullable: false),
                AttendanceStatus = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Attendance", x => x.Id);
                table.ForeignKey(
                    name: "FK_Attendance_Lecture_LectureId",
                    column: x => x.LectureId,
                    principalTable: "Lecture",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Attendance_Student_StudentId",
                    column: x => x.StudentId,
                    principalTable: "Student",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "LectureNote",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                LectureId = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Content = table.Column<string>(type: "text", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LectureNote", x => x.Id);
                table.ForeignKey(
                    name: "FK_LectureNote_Lecture_LectureId",
                    column: x => x.LectureId,
                    principalTable: "Lecture",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AssignmentSubmission",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                AssignmentId = table.Column<int>(type: "integer", nullable: false),
                StudentId = table.Column<int>(type: "integer", nullable: false),
                FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                Grade = table.Column<int>(type: "integer", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CreatedById = table.Column<int>(type: "integer", nullable: true),
                UpdatedById = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AssignmentSubmission", x => x.Id);
                table.ForeignKey(
                    name: "FK_AssignmentSubmission_Assignment_AssignmentId",
                    column: x => x.AssignmentId,
                    principalTable: "Assignment",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AssignmentSubmission_Student_StudentId",
                    column: x => x.StudentId,
                    principalTable: "Student",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "Address",
            columns: new[] { "Id", "City", "Number", "Street" },
            values: new object[,]
            {
                { 1, "Sarajevo", "12", "Bistrik" },
                { 2, "Sarajevo", "15", "Maršala Tita" },
                { 3, "Sarajevo", "8", "Mula Mustafe Bašeskije" },
                { 4, "Sarajevo", "18", "Obala Kulina bana" },
                { 5, "Sarajevo", "14", "Veliki Alifakovac" }
            });

        migrationBuilder.InsertData(
            table: "InstrumentType",
            columns: new[] { "Id", "MonthlyFee", "Type" },
            values: new object[,]
            {
                { 1, 45m, "Žičani" },
                { 2, 35m, "Udaraljke" },
                { 3, 55m, "Limeni" },
                { 4, 65m, "Tipke" },
                { 5, 15m, "Dodatna oprema" }
            });

        migrationBuilder.CreateIndex(
            name: "IX_Announcement_CourseId",
            table: "Announcement",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_Announcement_CreatedById",
            table: "Announcement",
            column: "CreatedById");

        migrationBuilder.CreateIndex(
            name: "IX_Announcement_MusicStoreId",
            table: "Announcement",
            column: "MusicStoreId");

        migrationBuilder.CreateIndex(
            name: "IX_Announcement_PublishedAt",
            table: "Announcement",
            column: "PublishedAt");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetRoleClaims_RoleId",
            table: "AspNetRoleClaims",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            table: "AspNetRoles",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserClaims_UserId",
            table: "AspNetUserClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserLogins_UserId",
            table: "AspNetUserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserRoles_RoleId",
            table: "AspNetUserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "AspNetUsers",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_AddressId",
            table: "AspNetUsers",
            column: "AddressId");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "AspNetUsers",
            column: "NormalizedUserName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Assignment_LectureId",
            table: "Assignment",
            column: "LectureId");

        migrationBuilder.CreateIndex(
            name: "IX_AssignmentSubmission_AssignmentId",
            table: "AssignmentSubmission",
            column: "AssignmentId");

        migrationBuilder.CreateIndex(
            name: "IX_AssignmentSubmission_StudentId",
            table: "AssignmentSubmission",
            column: "StudentId");

        migrationBuilder.CreateIndex(
            name: "IX_Attendance_LectureId",
            table: "Attendance",
            column: "LectureId");

        migrationBuilder.CreateIndex(
            name: "IX_Attendance_StudentId_LectureId",
            table: "Attendance",
            columns: new[] { "StudentId", "LectureId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Course_InstructorId",
            table: "Course",
            column: "InstructorId");

        migrationBuilder.CreateIndex(
            name: "IX_Enrollment_CourseId",
            table: "Enrollment",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_Enrollment_StudentId_CourseId",
            table: "Enrollment",
            columns: new[] { "StudentId", "CourseId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Instructor_AppUserId",
            table: "Instructor",
            column: "AppUserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Instrument_InstrumentTypeId",
            table: "Instrument",
            column: "InstrumentTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_Instrument_MusicStoreId",
            table: "Instrument",
            column: "MusicStoreId");

        migrationBuilder.CreateIndex(
            name: "IX_InstrumentRental_InstrumentId",
            table: "InstrumentRental",
            column: "InstrumentId",
            unique: true,
            filter: "\"RentalStatus\" IN (2, 3)");

        migrationBuilder.CreateIndex(
            name: "IX_InstrumentRental_StudentProfileId",
            table: "InstrumentRental",
            column: "StudentProfileId");

        migrationBuilder.CreateIndex(
            name: "IX_InstrumentView_LastViewedAt",
            table: "InstrumentView",
            column: "LastViewedAt");

        migrationBuilder.CreateIndex(
            name: "IX_InstrumentView_UserId_InstrumentId",
            table: "InstrumentView",
            columns: new[] { "UserId", "InstrumentId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Lecture_CourseId",
            table: "Lecture",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_LectureNote_LectureId",
            table: "LectureNote",
            column: "LectureId");

        migrationBuilder.CreateIndex(
            name: "IX_MusicStoreEmployee_AppUserId",
            table: "MusicStoreEmployee",
            column: "AppUserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MusicStoreEmployee_MusicStoreId_AppUserId",
            table: "MusicStoreEmployee",
            columns: new[] { "MusicStoreId", "AppUserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Notification_CreatedAt",
            table: "Notification",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Notification_UserId_IsRead",
            table: "Notification",
            columns: new[] { "UserId", "IsRead" });

        migrationBuilder.CreateIndex(
            name: "IX_Notification_UserId_RentalId_Title",
            table: "Notification",
            columns: new[] { "UserId", "RentalId", "Title" });

        migrationBuilder.CreateIndex(
            name: "IX_RentalNotificationOutbox_PublishedAt",
            table: "RentalNotificationOutbox",
            column: "PublishedAt");

        migrationBuilder.CreateIndex(
            name: "IX_RevokedToken_Jti",
            table: "RevokedToken",
            column: "Jti",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Student_AppUserId",
            table: "Student",
            column: "AppUserId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Announcement");

        migrationBuilder.DropTable(
            name: "AspNetRoleClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserLogins");

        migrationBuilder.DropTable(
            name: "AspNetUserRoles");

        migrationBuilder.DropTable(
            name: "AspNetUserTokens");

        migrationBuilder.DropTable(
            name: "AssignmentSubmission");

        migrationBuilder.DropTable(
            name: "Attendance");

        migrationBuilder.DropTable(
            name: "Enrollment");

        migrationBuilder.DropTable(
            name: "InstrumentRental");

        migrationBuilder.DropTable(
            name: "InstrumentView");

        migrationBuilder.DropTable(
            name: "LectureNote");

        migrationBuilder.DropTable(
            name: "MusicStoreEmployee");

        migrationBuilder.DropTable(
            name: "Notification");

        migrationBuilder.DropTable(
            name: "RentalNotificationOutbox");

        migrationBuilder.DropTable(
            name: "RevokedToken");

        migrationBuilder.DropTable(
            name: "AspNetRoles");

        migrationBuilder.DropTable(
            name: "Assignment");

        migrationBuilder.DropTable(
            name: "Instrument");

        migrationBuilder.DropTable(
            name: "Student");

        migrationBuilder.DropTable(
            name: "Lecture");

        migrationBuilder.DropTable(
            name: "InstrumentType");

        migrationBuilder.DropTable(
            name: "MusicStore");

        migrationBuilder.DropTable(
            name: "Course");

        migrationBuilder.DropTable(
            name: "Instructor");

        migrationBuilder.DropTable(
            name: "AspNetUsers");

        migrationBuilder.DropTable(
            name: "Address");
    }
}
