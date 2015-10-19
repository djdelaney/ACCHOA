﻿using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<History> Histories { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<Response> Responses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Comment>().HasKey(v => v.Id);
            builder.Entity<History>().HasKey(v => v.Id);
            builder.Entity<ApplicationUser>().HasKey(v => v.Id);
            builder.Entity<Review>().HasKey(v => v.Id);
            builder.Entity<Submission>().HasKey(v => v.Id);
            builder.Entity<File>().HasKey(v => v.Id);
            builder.Entity<Response>().HasKey(v => v.Id);

            builder.Entity<Submission>().HasMany(s => s.Audits).WithOne(h => h.Submission).Required(false);

            //var builder = modelBuilder.Entity<CustomerDetails>().Reference(e => e.Customer).InverseReference(e => e.Details);
            //builder.Entity<Submission>().Reference

            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
