using DepartmentFinancialRecords.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DepartmentFinancialRecords.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<YearLevel> YearLevels => Set<YearLevel>();
        public DbSet<DepartmentBill> DepartmentBills => Set<DepartmentBill>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Fine> Fines => Set<Fine>();
        public DbSet<Collection> Collections => Set<Collection>();
        public DbSet<Collectible> Collectibles => Set<Collectible>();
        public DbSet<FundTransaction> FundTransactions => Set<FundTransaction>();
        public DbSet<Disbursement> Disbursements => Set<Disbursement>();
        public DbSet<AttendanceEvent> AttendanceEvents => Set<AttendanceEvent>();
        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Student>()
                .HasIndex(student => student.StudentId)
                .IsUnique();

            modelBuilder.Entity<Course>()
                .HasIndex(course => course.Name)
                .IsUnique();

            modelBuilder.Entity<YearLevel>()
                .HasIndex(yearLevel => yearLevel.Name)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasOne(student => student.CourseOption)
                .WithMany()
                .HasForeignKey(student => student.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(student => student.YearLevelOption)
                .WithMany()
                .HasForeignKey(student => student.YearLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Collectible>()
                .HasOne(collectible => collectible.DepartmentBill)
                .WithMany()
                .HasForeignKey(collectible => collectible.DepartmentBillId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Student>()
                .HasIndex(student => student.RfidUid);

            modelBuilder.Entity<AttendanceRecord>()
                .HasIndex(record => new { record.StudentId, record.AttendanceEventId })
                .IsUnique();
        }
    }
}
