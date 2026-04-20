using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentEnrollmentSystem.Models;

namespace StudentEnrollmentSystem.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<AddDropAudit> AddDropAudits => Set<AddDropAudit>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseSection> CourseSections => Set<CourseSection>();
    public DbSet<EnrollmentRecord> EnrollmentRecords => Set<EnrollmentRecord>();
    public DbSet<SectionMeeting> SectionMeetings => Set<SectionMeeting>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<ContactUsMessage> ContactUsMessages => Set<ContactUsMessage>();
    public DbSet<TeachingEvaluation> TeachingEvaluations => Set<TeachingEvaluation>();
    public DbSet<Payment> Payments => Set<Payment>();
    

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(120);
            entity.Property(user => user.Address).HasMaxLength(200);
            entity.Property(user => user.Postcode).HasMaxLength(20);
            entity.Property(user => user.City).HasMaxLength(100);
            entity.Property(user => user.State).HasMaxLength(100);
            entity.Property(user => user.Country).HasMaxLength(100);
            entity.Property(user => user.BankName).HasMaxLength(120);
            entity.Property(user => user.EncryptedBankAccountNumber).HasMaxLength(512);
            entity.Property(user => user.EncryptedBankAccountHolderName).HasMaxLength(512);
        });

        builder.Entity<StudentProfile>(entity =>
        {
            entity.HasIndex(profile => profile.ApplicationUserId).IsUnique();
            entity.HasIndex(profile => profile.StudentNumber).IsUnique();
            entity.Property(profile => profile.StudentNumber).HasMaxLength(20);
            entity.Property(profile => profile.FullName).HasMaxLength(120);
            entity.Property(profile => profile.ProgramName).HasMaxLength(120);
            entity.Property(profile => profile.ProgramCode).HasMaxLength(20);
            entity.Property(profile => profile.IntakeLabel).HasMaxLength(40);

            entity.HasOne(profile => profile.ApplicationUser)
                .WithOne(user => user.StudentProfile)
                .HasForeignKey<StudentProfile>(profile => profile.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(profile => profile.CurrentSemester)
                .WithMany()
                .HasForeignKey(profile => profile.CurrentSemesterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Semester>(entity =>
        {
            entity.HasIndex(semester => semester.Code).IsUnique();
            entity.Property(semester => semester.Code).HasMaxLength(30);
            entity.Property(semester => semester.Name).HasMaxLength(80);
            entity.Property(semester => semester.Status).HasConversion<int>();
        });

        builder.Entity<Course>(entity =>
        {
            entity.HasIndex(course => course.Code).IsUnique();
            entity.Property(course => course.Code).HasMaxLength(20);
            entity.Property(course => course.Title).HasMaxLength(120);
            entity.Property(course => course.EligibleProgrammeCodes).HasMaxLength(80);
        });

        builder.Entity<CourseSection>(entity =>
        {
            entity.Property(section => section.SectionCode).HasMaxLength(20);
            entity.Property(section => section.InstructorName).HasMaxLength(120);
            entity.HasIndex(section => new { section.SemesterId, section.CourseId, section.SectionCode }).IsUnique();
        });

        builder.Entity<SectionMeeting>(entity =>
        {
            entity.Property(meeting => meeting.DayOfWeek).HasConversion<int>();
            entity.Property(meeting => meeting.Venue).HasMaxLength(80);
            entity.Property(meeting => meeting.StartTime).HasColumnType("time");
            entity.Property(meeting => meeting.EndTime).HasColumnType("time");
        });

        builder.Entity<EnrollmentRecord>(entity =>
        {
            entity.Property(record => record.Status).HasConversion<int>();
            entity.Property(record => record.DropReason).HasMaxLength(300);
        });

        builder.Entity<AddDropAudit>(entity =>
        {
            entity.Property(audit => audit.ActionType).HasConversion<int>();
            entity.Property(audit => audit.Remarks).HasMaxLength(300);
        });
    }
}
