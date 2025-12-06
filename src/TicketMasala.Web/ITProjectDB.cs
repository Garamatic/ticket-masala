using System.Data.Common;
using TicketMasala.Web.Models;
using TicketMasala.Web.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TicketMasala.Web;
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

            // Configure Project-Customer Many-to-Many relationship (Stakeholders)
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Customers)
                .WithMany() // Unidirectional: ApplicationUser does not have Projects collection
                .UsingEntity(j => j.ToTable("ProjectCustomers"));

            // Configure Project-Customer One-to-Many relationship (Primary Owner)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Customer)
                .WithMany()
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Employee> Employees { get; set; }
        // Customer deleted
        public DbSet<Document> Documents { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLogEntry> AuditLogs { get; set; }
        public DbSet<TicketComment> TicketComments { get; set; }
        // Department deleted
        public DbSet<KnowledgeBaseArticle> KnowledgeBaseArticles { get; set; }
        // QualityReview deleted
        public DbSet<SavedFilter> SavedFilters { get; set; }
        public DbSet<ProjectTemplate> ProjectTemplates { get; set; }
        public DbSet<TemplateTicket> TemplateTickets { get; set; }

    }
