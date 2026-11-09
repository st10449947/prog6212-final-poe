using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<RaceRoute> Routes { get; set; }
    public DbSet<Enrolment> Enrolments { get; set; }
    public DbSet<Result> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Users: unique email
        b.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        b.Entity<AppUser>().Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");

        // Organiser (1) -> Events (many)
        b.Entity<Event>()
            .HasOne(e => e.Organiser).WithMany(u => u.Events)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        b.Entity<Event>().Property(e => e.Distance).HasPrecision(6, 2);
        b.Entity<Event>().Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

        // Event (1) -> Categories (many)
        b.Entity<Category>()
            .HasOne(c => c.Event).WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<Category>().Property(c => c.DistanceKm).HasPrecision(6, 2);

        // Category (1) <-> Route (1)
        b.Entity<RaceRoute>()
            .HasOne(r => r.Category).WithOne(c => c.Route)
            .HasForeignKey<RaceRoute>(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enrolment links Participant + Event + Category
        b.Entity<Enrolment>()
            .HasOne(e => e.Participant).WithMany(u => u.Enrolments)
            .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Enrolment>()
            .HasOne(e => e.Event).WithMany(ev => ev.Enrolments)
            .HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Enrolment>()
            .HasOne(e => e.Category).WithMany(c => c.Enrolments)
            .HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Enrolment>().HasIndex(e => new { e.UserId, e.EventId }).IsUnique();
        b.Entity<Enrolment>().Property(e => e.Status).HasDefaultValue("Confirmed");
        b.Entity<Enrolment>().Property(e => e.EnrolmentDate).HasDefaultValueSql("GETDATE()");

        // Enrolment (1) <-> Result (0..1)
        b.Entity<Result>()
            .HasOne(r => r.Enrolment).WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Entity<Result>()
            .HasOne(r => r.RecordedBy).WithMany()
            .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Result>().Property(r => r.RecordedAt).HasDefaultValueSql("GETDATE()");
    }
}