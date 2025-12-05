using System.Data.Common;
using IT_Project2526.Models;
using IT_Project2526.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IT_Project2526
{
    public class ITProjectDB : IdentityDbContext<ApplicationUser>
    {
        public ITProjectDB(DbContextOptions<ITProjectDB> options)
    : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            // Suppress PendingModelChangesWarning during migrations on Fly
            optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //roles
            var admin = new IdentityRole
            {
                Id = "1",
                Name = Constants.RoleAdmin,
                NormalizedName = Constants.RoleAdmin.ToUpper()
            };

            var employee = new IdentityRole
            {
                Id = "2",
                Name = Constants.RoleEmployee,
                NormalizedName = Constants.RoleEmployee.ToUpper()
            };

            var cust = new IdentityRole
            {
                Id = "3",
                Name = Constants.RoleCustomer,
                NormalizedName = Constants.RoleCustomer.ToUpper()
            };

            modelBuilder.Entity<IdentityRole>()
                .HasData(employee, admin, cust);

            // Configure Project-Ticket relationship
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectGuid)
                .OnDelete(DeleteBehavior.SetNull);

        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLogEntry> AuditLogs { get; set; }
        public DbSet<TicketComment> TicketComments { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles { get; set; }
        public DbSet<QualityReview> QualityReviews { get; set; }
        public DbSet<SavedFilter> SavedFilters { get; set; }

    }
}
