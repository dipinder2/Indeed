using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Indeed.Models;

public class JobSiteContext : DbContext
{
    public JobSiteContext(DbContextOptions<JobSiteContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        /*
         * M:M between User & Jobs
         * for each job has many candidates
         * as for one candiadate there are many jobs
         * flow is back and forth
         */
        modelBuilder.Entity<User>()
            .HasMany(u => u.AppliedJobs)
            .WithMany(aj => aj.Candidates);
        modelBuilder.Entity<Job>()
            .HasMany(j=>j.Candidates)
            .WithMany(u => u.AppliedJobs);
        /*
         * 1:M between User & Jobs
         * as each job can be created by only one User
         * but User can create many jobs
         * flow is back and forth
         */
        modelBuilder.Entity<User>()
            .HasMany<Job>(u => u.JobsCreated)
            .WithOne(j => j.User);
        modelBuilder.Entity<Job>()
            .HasOne(job => job.User)
            .WithMany(u => u.JobsCreated);
        /*
         * 1:1 between User & Resume
         */
        // modelBuilder.Entity<User>()
        //     .HasOne(u => u.Resume);
    }

    public DbSet<IdentityUserClaim<string>> IdentityUserClaim { get; set; }
    public DbSet<User> Users { get; set; }
    // public DbSet<Resume> Resumes { get; set; }
    public DbSet<Job> Jobs { get; set; }
}