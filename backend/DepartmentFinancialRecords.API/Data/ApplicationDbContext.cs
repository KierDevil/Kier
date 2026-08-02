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

            modelBuilder.Entity<Student>()
                .HasIndex(student => student.RfidUid);

            modelBuilder.Entity<AttendanceRecord>()
                .HasIndex(record => new { record.StudentId, record.AttendanceEventId })
                .IsUnique();
        }
    }
}
