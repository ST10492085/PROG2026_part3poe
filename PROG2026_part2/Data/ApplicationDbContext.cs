using Microsoft.EntityFrameworkCore;
using PROG2026_part2.Models;

namespace PROG2026_part2.Data;

/// <summary>
/// Entity Framework Core database context (Code-First). Each DbSet below becomes a table,
/// and OnModelCreating describes the keys, constraints and relationships of the schema
/// from the Part 1 ERD.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Tables
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ----- Users -----
        // Email must be unique because it is the login name.
        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<ApplicationUser>()
            .Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(150);

        modelBuilder.Entity<ApplicationUser>()
            .Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(20);

        // Image URLs point to files in Azure Blob Storage.
        modelBuilder.Entity<ApplicationUser>()
            .Property(u => u.ProfilePictureUrl)
            .HasMaxLength(500);

        // ----- Events -----
        modelBuilder.Entity<Event>()
            .Property(e => e.BannerImageUrl)
            .HasMaxLength(500);

        modelBuilder.Entity<Event>()
            .Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(150);

        modelBuilder.Entity<Event>()
            .Property(e => e.EventType)
            .HasMaxLength(20);

        // An event belongs to one Organizer. Restrict = an Organizer cannot be deleted while they own events.
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organizer)
            .WithMany()
            .HasForeignKey(e => e.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ----- Categories -----
        // A category belongs to one event; deleting an event deletes its categories (Cascade).
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Two categories in the same event cannot share a name.
        modelBuilder.Entity<Category>()
            .HasIndex(c => new { c.EventId, c.Name })
            .IsUnique();

        // ----- Enrollments -----
        // Restrict is used on all three foreign keys to avoid SQL Server "multiple cascade paths"
        // errors and to stop data being silently removed.
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.User)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Event)
            .WithMany(e => e.Enrollments)
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Category)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // A participant can enter a given event only once.
        modelBuilder.Entity<Enrollment>()
            .HasIndex(e => new { e.UserId, e.EventId })
            .IsUnique();

        // ----- Results -----
        // Restrict on all foreign keys: results are never removed automatically.
        modelBuilder.Entity<Result>()
            .HasOne(r => r.User)
            .WithMany(u => u.Results)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Result>()
            .HasOne(r => r.Event)
            .WithMany(e => e.Results)
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Result>()
            .HasOne(r => r.Category)
            .WithMany(c => c.Results)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}