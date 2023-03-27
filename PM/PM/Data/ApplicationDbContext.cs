using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PM.Models;

namespace PM.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        //public DbSet<PM.ViewModels.AccountDetailsViewModel> AccountDetailsViewModel { get; set; }
        public DbSet<PM.Models.FileDB> FileDB { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //builder.Entity<AccountDetailsViewModel>().ToTable("AccountDetailsViewModel");
            builder.Entity<FileDB>().ToTable("FileDB");
        }

        public DbSet<PM.Models.UserAccount> UserAccount { get; set; }
    }
}