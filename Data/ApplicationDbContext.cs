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

        builder.Entity<Course>().HasData(
        new Course { Id = 1, Code = "CSC101", Title = "Introduction to Programming", Fee = 1000 },
        new Course { Id = 2, Code = "CSC102", Title = "Web Development Fundamentals", Fee = 1200 },
        new Course { Id = 3, Code = "CSC201", Title = "Object-Oriented Programming", Fee = 1100 },
        new Course { Id = 4, Code = "CSC230", Title = "Database Systems", Fee = 1300 },
        new Course { Id = 5, Code = "CSC240", Title = "Computer Networks", Fee = 1250 },
        new Course { Id = 6, Code = "CSC245", Title = "Cloud Fundamentals", Fee = 1400 },
        new Course { Id = 7, Code = "CSC310", Title = "Data Structures and Algorithms", Fee = 1500 },
        new Course { Id = 8, Code = "AIS260", Title = "Applied Business Analytics", Fee = 1350 },
        new Course { Id = 9, Code = "DAT250", Title = "Data Visualization", Fee = 1150 },
        new Course { Id = 10, Code = "CLD270", Title = "Cloud Infrastructure Services", Fee = 1600 },
        new Course { Id = 11, Code = "CYB220", Title = "Cyber Security Fundamentals", Fee = 1700 },
        new Course { Id = 12, Code = "MOB230", Title = "Mobile App Development", Fee = 1550 },
        new Course { Id = 13, Code = "UXD210", Title = "User Experience Design", Fee = 1450 },
        new Course { Id = 14, Code = "MAT201", Title = "Discrete Mathematics", Fee = 1800 },
        new Course { Id = 15, Code = "MAT210", Title = "Applied Calculus", Fee = 1750 },
        new Course { Id = 16, Code = "STA210", Title = "Business Statistics", Fee = 1900 },
        new Course { Id = 17, Code = "ENG150", Title = "Academic Writing", Fee = 2000 },
        new Course { Id = 18, Code = "COM110", Title = "Communication Skills", Fee = 2100 },
        new Course { Id = 19, Code = "HIS220", Title = "Malaysian Civilisation", Fee = 2200 },
        new Course { Id = 20, Code = "LAW160", Title = "Business Law", Fee = 2300 },
        new Course { Id = 21, Code = "ACC110", Title = "Accounting Principles", Fee = 2400 },
        new Course { Id = 22, Code = "BUS205", Title = "Entrepreneurship", Fee = 2500 },
        new Course { Id = 23, Code = "ECO120", Title = "Microeconomics", Fee = 2600 },
        new Course { Id = 24, Code = "FIN215", Title = "Personal Finance", Fee = 2700 },
        new Course { Id = 25, Code = "MKT225", Title = "Digital Marketing", Fee = 2800 }
    );
    }
}
