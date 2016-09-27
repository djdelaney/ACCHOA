using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        protected ApplicationDbContext()
        {
        }

        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<History> Histories { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<Response> Responses { get; set; }
        public DbSet<StateChange> StateChanges { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Comment>().HasKey(v => v.Id);
            builder.Entity<History>().HasKey(v => v.Id);
            builder.Entity<ApplicationUser>().HasKey(v => v.Id);
            builder.Entity<Review>().HasKey(v => v.Id);
            builder.Entity<Submission>().HasKey(v => v.Id);
            builder.Entity<File>().HasKey(v => v.Id);
            builder.Entity<Response>().HasKey(v => v.Id);
            builder.Entity<StateChange>().HasKey(v => v.Id);
            
            builder.Entity<Submission>().HasMany(s => s.Reviews).WithOne(h => h.Submission).IsRequired(false);
            builder.Entity<Submission>().HasMany(s => s.Audits).WithOne(h => h.Submission).IsRequired(false);
            builder.Entity<Submission>().HasMany(s => s.Responses).WithOne(h => h.Submission).IsRequired(false);
            builder.Entity<Submission>().HasMany(s => s.Files).WithOne(h => h.Submission).IsRequired(false);
            builder.Entity<Submission>().HasMany(s => s.StateHistory).WithOne(h => h.Submission).IsRequired(false);
            builder.Entity<Submission>().HasMany(s => s.Comments).WithOne(h => h.Submission).IsRequired(false);

            //var builder = modelBuilder.Entity<CustomerDetails>().Reference(e => e.Customer).InverseReference(e => e.Details);
            //builder.Entity<Submission>().Reference

            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
