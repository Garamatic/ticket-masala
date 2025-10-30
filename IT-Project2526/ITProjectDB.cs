using System.Data.Common;
using IT_Project2526.Models;
using IT_Project2526.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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

        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Customer> Customers { get; set; }

    }
}
