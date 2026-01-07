using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TicketMasala.Domain.Data;

namespace TicketMasala.Web.Data
{
    public class MasalaDbContextFactory : IDesignTimeDbContextFactory<MasalaDbContext>
    {
        public MasalaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MasalaDbContext>();
            optionsBuilder.UseSqlite("Data Source=app.db", b => b.MigrationsAssembly("TicketMasala.Web"));

            return new MasalaDbContext(optionsBuilder.Options);
        }
    }
}
