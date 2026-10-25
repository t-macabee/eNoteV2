using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eNote.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuditUserForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Student_CreatedById",
                table: "Student",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Student_UpdatedById",
                table: "Student",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEvent_CreatedById",
                table: "StripeWebhookEvent",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_StripeWebhookEvent_UpdatedById",
                table: "StripeWebhookEvent",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RentalPayment_CreatedById",
                table: "RentalPayment",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RentalPayment_UpdatedById",
                table: "RentalPayment",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RentalNotificationOutbox_CreatedById",
                table: "RentalNotificationOutbox",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_RentalNotificationOutbox_UpdatedById",
                table: "RentalNotificationOutbox",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MusicStoreEmployee_CreatedById",
                table: "MusicStoreEmployee",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MusicStoreEmployee_UpdatedById",
                table: "MusicStoreEmployee",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MusicStore_CreatedById",
                table: "MusicStore",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MusicStore_UpdatedById",
                table: "MusicStore",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPayment_CreatedById",
                table: "MembershipPayment",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipPayment_UpdatedById",
                table: "MembershipPayment",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_LectureNote_CreatedById",
                table: "LectureNote",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_LectureNote_UpdatedById",
                table: "LectureNote",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Lecture_CreatedById",
                table: "Lecture",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Lecture_UpdatedById",
                table: "Lecture",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentRental_CreatedById",
                table: "InstrumentRental",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentRental_UpdatedById",
                table: "InstrumentRental",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Instrument_CreatedById",
                table: "Instrument",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Instrument_UpdatedById",
                table: "Instrument",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Instructor_CreatedById",
                table: "Instructor",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Instructor_UpdatedById",
                table: "Instructor",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Event_UpdatedById",
                table: "Event",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_CreatedById",
                table: "Enrollment",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_DecidedById",
                table: "Enrollment",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollment_UpdatedById",
                table: "Enrollment",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CoursePayment_CreatedById",
                table: "CoursePayment",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_CoursePayment_UpdatedById",
                table: "CoursePayment",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Course_CreatedById",
                table: "Course",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Course_UpdatedById",
                table: "Course",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_CreatedById",
                table: "Attendance",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_UpdatedById",
                table: "Attendance",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmission_CreatedById",
                table: "AssignmentSubmission",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmission_UpdatedById",
                table: "AssignmentSubmission",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Assignment_CreatedById",
                table: "Assignment",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Assignment_UpdatedById",
                table: "Assignment",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Announcement_UpdatedById",
                table: "Announcement",
                column: "UpdatedById");

            migrationBuilder.Sql(
                """
                UPDATE [Announcement] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Announcement_AspNetUsers_UpdatedById",
                table: "Announcement",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Assignment] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_AspNetUsers_CreatedById",
                table: "Assignment",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Assignment] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignment_AspNetUsers_UpdatedById",
                table: "Assignment",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [AssignmentSubmission] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentSubmission_AspNetUsers_CreatedById",
                table: "AssignmentSubmission",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [AssignmentSubmission] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_AssignmentSubmission_AspNetUsers_UpdatedById",
                table: "AssignmentSubmission",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Attendance] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_AspNetUsers_CreatedById",
                table: "Attendance",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Attendance] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_AspNetUsers_UpdatedById",
                table: "Attendance",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Course] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Course_AspNetUsers_CreatedById",
                table: "Course",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Course] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Course_AspNetUsers_UpdatedById",
                table: "Course",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [CoursePayment] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_CoursePayment_AspNetUsers_CreatedById",
                table: "CoursePayment",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [CoursePayment] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_CoursePayment_AspNetUsers_UpdatedById",
                table: "CoursePayment",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Enrollment] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollment_AspNetUsers_CreatedById",
                table: "Enrollment",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Enrollment] SET [DecidedById] = NULL WHERE [DecidedById] IS NOT NULL AND [DecidedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollment_AspNetUsers_DecidedById",
                table: "Enrollment",
                column: "DecidedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Enrollment] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollment_AspNetUsers_UpdatedById",
                table: "Enrollment",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Event] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Event_AspNetUsers_UpdatedById",
                table: "Event",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Instructor] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Instructor_AspNetUsers_CreatedById",
                table: "Instructor",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Instructor] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Instructor_AspNetUsers_UpdatedById",
                table: "Instructor",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Instrument] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Instrument_AspNetUsers_CreatedById",
                table: "Instrument",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Instrument] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Instrument_AspNetUsers_UpdatedById",
                table: "Instrument",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [InstrumentRental] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_InstrumentRental_AspNetUsers_CreatedById",
                table: "InstrumentRental",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [InstrumentRental] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_InstrumentRental_AspNetUsers_UpdatedById",
                table: "InstrumentRental",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Lecture] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Lecture_AspNetUsers_CreatedById",
                table: "Lecture",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Lecture] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Lecture_AspNetUsers_UpdatedById",
                table: "Lecture",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [LectureNote] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_LectureNote_AspNetUsers_CreatedById",
                table: "LectureNote",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [LectureNote] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_LectureNote_AspNetUsers_UpdatedById",
                table: "LectureNote",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [MembershipPayment] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MembershipPayment_AspNetUsers_CreatedById",
                table: "MembershipPayment",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [MembershipPayment] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MembershipPayment_AspNetUsers_UpdatedById",
                table: "MembershipPayment",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [MusicStore] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MusicStore_AspNetUsers_CreatedById",
                table: "MusicStore",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [MusicStore] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MusicStore_AspNetUsers_UpdatedById",
                table: "MusicStore",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [MusicStoreEmployee] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MusicStoreEmployee_AspNetUsers_CreatedById",
                table: "MusicStoreEmployee",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [MusicStoreEmployee] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MusicStoreEmployee_AspNetUsers_UpdatedById",
                table: "MusicStoreEmployee",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [RentalNotificationOutbox] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalNotificationOutbox_AspNetUsers_CreatedById",
                table: "RentalNotificationOutbox",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [RentalNotificationOutbox] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalNotificationOutbox_AspNetUsers_UpdatedById",
                table: "RentalNotificationOutbox",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [RentalPayment] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalPayment_AspNetUsers_CreatedById",
                table: "RentalPayment",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [RentalPayment] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalPayment_AspNetUsers_UpdatedById",
                table: "RentalPayment",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [StripeWebhookEvent] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_StripeWebhookEvent_AspNetUsers_CreatedById",
                table: "StripeWebhookEvent",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [StripeWebhookEvent] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_StripeWebhookEvent_AspNetUsers_UpdatedById",
                table: "StripeWebhookEvent",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Student] SET [CreatedById] = NULL WHERE [CreatedById] IS NOT NULL AND [CreatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Student_AspNetUsers_CreatedById",
                table: "Student",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                UPDATE [Student] SET [UpdatedById] = NULL WHERE [UpdatedById] IS NOT NULL AND [UpdatedById] NOT IN (SELECT [Id] FROM [AspNetUsers])
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Student_AspNetUsers_UpdatedById",
                table: "Student",
                column: "UpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Announcement_AspNetUsers_UpdatedById",
                table: "Announcement");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignment_AspNetUsers_CreatedById",
                table: "Assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_Assignment_AspNetUsers_UpdatedById",
                table: "Assignment");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentSubmission_AspNetUsers_CreatedById",
                table: "AssignmentSubmission");

            migrationBuilder.DropForeignKey(
                name: "FK_AssignmentSubmission_AspNetUsers_UpdatedById",
                table: "AssignmentSubmission");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_AspNetUsers_CreatedById",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_AspNetUsers_UpdatedById",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK_Course_AspNetUsers_CreatedById",
                table: "Course");

            migrationBuilder.DropForeignKey(
                name: "FK_Course_AspNetUsers_UpdatedById",
                table: "Course");

            migrationBuilder.DropForeignKey(
                name: "FK_CoursePayment_AspNetUsers_CreatedById",
                table: "CoursePayment");

            migrationBuilder.DropForeignKey(
                name: "FK_CoursePayment_AspNetUsers_UpdatedById",
                table: "CoursePayment");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollment_AspNetUsers_CreatedById",
                table: "Enrollment");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollment_AspNetUsers_DecidedById",
                table: "Enrollment");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollment_AspNetUsers_UpdatedById",
                table: "Enrollment");

            migrationBuilder.DropForeignKey(
                name: "FK_Event_AspNetUsers_UpdatedById",
                table: "Event");

            migrationBuilder.DropForeignKey(
                name: "FK_Instructor_AspNetUsers_CreatedById",
                table: "Instructor");

            migrationBuilder.DropForeignKey(
                name: "FK_Instructor_AspNetUsers_UpdatedById",
                table: "Instructor");

            migrationBuilder.DropForeignKey(
                name: "FK_Instrument_AspNetUsers_CreatedById",
                table: "Instrument");

            migrationBuilder.DropForeignKey(
                name: "FK_Instrument_AspNetUsers_UpdatedById",
                table: "Instrument");

            migrationBuilder.DropForeignKey(
                name: "FK_InstrumentRental_AspNetUsers_CreatedById",
                table: "InstrumentRental");

            migrationBuilder.DropForeignKey(
                name: "FK_InstrumentRental_AspNetUsers_UpdatedById",
                table: "InstrumentRental");

            migrationBuilder.DropForeignKey(
                name: "FK_Lecture_AspNetUsers_CreatedById",
                table: "Lecture");

            migrationBuilder.DropForeignKey(
                name: "FK_Lecture_AspNetUsers_UpdatedById",
                table: "Lecture");

            migrationBuilder.DropForeignKey(
                name: "FK_LectureNote_AspNetUsers_CreatedById",
                table: "LectureNote");

            migrationBuilder.DropForeignKey(
                name: "FK_LectureNote_AspNetUsers_UpdatedById",
                table: "LectureNote");

            migrationBuilder.DropForeignKey(
                name: "FK_MembershipPayment_AspNetUsers_CreatedById",
                table: "MembershipPayment");

            migrationBuilder.DropForeignKey(
                name: "FK_MembershipPayment_AspNetUsers_UpdatedById",
                table: "MembershipPayment");

            migrationBuilder.DropForeignKey(
                name: "FK_MusicStore_AspNetUsers_CreatedById",
                table: "MusicStore");

            migrationBuilder.DropForeignKey(
                name: "FK_MusicStore_AspNetUsers_UpdatedById",
                table: "MusicStore");

            migrationBuilder.DropForeignKey(
                name: "FK_MusicStoreEmployee_AspNetUsers_CreatedById",
                table: "MusicStoreEmployee");

            migrationBuilder.DropForeignKey(
                name: "FK_MusicStoreEmployee_AspNetUsers_UpdatedById",
                table: "MusicStoreEmployee");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalNotificationOutbox_AspNetUsers_CreatedById",
                table: "RentalNotificationOutbox");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalNotificationOutbox_AspNetUsers_UpdatedById",
                table: "RentalNotificationOutbox");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalPayment_AspNetUsers_CreatedById",
                table: "RentalPayment");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalPayment_AspNetUsers_UpdatedById",
                table: "RentalPayment");

            migrationBuilder.DropForeignKey(
                name: "FK_StripeWebhookEvent_AspNetUsers_CreatedById",
                table: "StripeWebhookEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_StripeWebhookEvent_AspNetUsers_UpdatedById",
                table: "StripeWebhookEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_AspNetUsers_CreatedById",
                table: "Student");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_AspNetUsers_UpdatedById",
                table: "Student");

            migrationBuilder.DropIndex(
                name: "IX_Student_CreatedById",
                table: "Student");

            migrationBuilder.DropIndex(
                name: "IX_Student_UpdatedById",
                table: "Student");

            migrationBuilder.DropIndex(
                name: "IX_StripeWebhookEvent_CreatedById",
                table: "StripeWebhookEvent");

            migrationBuilder.DropIndex(
                name: "IX_StripeWebhookEvent_UpdatedById",
                table: "StripeWebhookEvent");

            migrationBuilder.DropIndex(
                name: "IX_RentalPayment_CreatedById",
                table: "RentalPayment");

            migrationBuilder.DropIndex(
                name: "IX_RentalPayment_UpdatedById",
                table: "RentalPayment");

            migrationBuilder.DropIndex(
                name: "IX_RentalNotificationOutbox_CreatedById",
                table: "RentalNotificationOutbox");

            migrationBuilder.DropIndex(
                name: "IX_RentalNotificationOutbox_UpdatedById",
                table: "RentalNotificationOutbox");

            migrationBuilder.DropIndex(
                name: "IX_MusicStoreEmployee_CreatedById",
                table: "MusicStoreEmployee");

            migrationBuilder.DropIndex(
                name: "IX_MusicStoreEmployee_UpdatedById",
                table: "MusicStoreEmployee");

            migrationBuilder.DropIndex(
                name: "IX_MusicStore_CreatedById",
                table: "MusicStore");

            migrationBuilder.DropIndex(
                name: "IX_MusicStore_UpdatedById",
                table: "MusicStore");

            migrationBuilder.DropIndex(
                name: "IX_MembershipPayment_CreatedById",
                table: "MembershipPayment");

            migrationBuilder.DropIndex(
                name: "IX_MembershipPayment_UpdatedById",
                table: "MembershipPayment");

            migrationBuilder.DropIndex(
                name: "IX_LectureNote_CreatedById",
                table: "LectureNote");

            migrationBuilder.DropIndex(
                name: "IX_LectureNote_UpdatedById",
                table: "LectureNote");

            migrationBuilder.DropIndex(
                name: "IX_Lecture_CreatedById",
                table: "Lecture");

            migrationBuilder.DropIndex(
                name: "IX_Lecture_UpdatedById",
                table: "Lecture");

            migrationBuilder.DropIndex(
                name: "IX_InstrumentRental_CreatedById",
                table: "InstrumentRental");

            migrationBuilder.DropIndex(
                name: "IX_InstrumentRental_UpdatedById",
                table: "InstrumentRental");

            migrationBuilder.DropIndex(
                name: "IX_Instrument_CreatedById",
                table: "Instrument");

            migrationBuilder.DropIndex(
                name: "IX_Instrument_UpdatedById",
                table: "Instrument");

            migrationBuilder.DropIndex(
                name: "IX_Instructor_CreatedById",
                table: "Instructor");

            migrationBuilder.DropIndex(
                name: "IX_Instructor_UpdatedById",
                table: "Instructor");

            migrationBuilder.DropIndex(
                name: "IX_Event_UpdatedById",
                table: "Event");

            migrationBuilder.DropIndex(
                name: "IX_Enrollment_CreatedById",
                table: "Enrollment");

            migrationBuilder.DropIndex(
                name: "IX_Enrollment_DecidedById",
                table: "Enrollment");

            migrationBuilder.DropIndex(
                name: "IX_Enrollment_UpdatedById",
                table: "Enrollment");

            migrationBuilder.DropIndex(
                name: "IX_CoursePayment_CreatedById",
                table: "CoursePayment");

            migrationBuilder.DropIndex(
                name: "IX_CoursePayment_UpdatedById",
                table: "CoursePayment");

            migrationBuilder.DropIndex(
                name: "IX_Course_CreatedById",
                table: "Course");

            migrationBuilder.DropIndex(
                name: "IX_Course_UpdatedById",
                table: "Course");

            migrationBuilder.DropIndex(
                name: "IX_Attendance_CreatedById",
                table: "Attendance");

            migrationBuilder.DropIndex(
                name: "IX_Attendance_UpdatedById",
                table: "Attendance");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentSubmission_CreatedById",
                table: "AssignmentSubmission");

            migrationBuilder.DropIndex(
                name: "IX_AssignmentSubmission_UpdatedById",
                table: "AssignmentSubmission");

            migrationBuilder.DropIndex(
                name: "IX_Assignment_CreatedById",
                table: "Assignment");

            migrationBuilder.DropIndex(
                name: "IX_Assignment_UpdatedById",
                table: "Assignment");

            migrationBuilder.DropIndex(
                name: "IX_Announcement_UpdatedById",
                table: "Announcement");
        }
    }
}
