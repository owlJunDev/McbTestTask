using Microsoft.EntityFrameworkCore;
 
namespace Server.Contexts
{
    public class ApplicationContext : DbContext
    {
        public DbSet<ResultGameList> ResultGameLists { get; set; }
        public ApplicationContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;" +
                                    "Port=5432;" +
                                    "Database=tictactoe;" +
                                    "Username=postgres;" +
                                    "Password=postgres");
        }
    }
}