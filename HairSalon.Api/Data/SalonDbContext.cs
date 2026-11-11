using HairSalon.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HairSalon.Api.Data;

/// <summary>
/// The DATA ACCESS LAYER: EF Core's gateway to the SQLite database.
/// It inherits IdentityDbContext so the Identity tables (AspNetUsers, AspNetRoles ...)
/// live in the same database as the salon's own tables.
/// Each DbSet&lt;T&gt; becomes a table.
/// </summary>
public class SalonDbContext(DbContextOptions<SalonDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Stylist> Stylists => Set<Stylist>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    /// <summary>
    /// The "Fluent API": configuration that cannot (or should not) be done with attributes,
    /// such as relationships and what happens when a row is deleted.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // VERY important: lets Identity configure its own tables first.
        base.OnModelCreating(builder);

        builder.Entity<Service>(entity =>
        {
            // Store the enum as text ("Haircut") instead of a number (0).
            entity.Property(s => s.Category).HasConversion<string>().HasMaxLength(30);
        });

        builder.Entity<Appointment>(entity =>
        {
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

            // Customer 1 --- * Appointment. Deleting a customer deletes their appointments.
            entity.HasOne(a => a.Customer)
                  .WithMany(u => u.Appointments)
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Service 1 --- * Appointment. RESTRICT: a service with bookings cannot be deleted
            // by accident (the service layer deactivates it instead).
            entity.HasOne(a => a.Service)
                  .WithMany(s => s.Appointments)
                  .HasForeignKey(a => a.ServiceId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Stylist 1 --- * Appointment. Same idea as for services.
            entity.HasOne(a => a.Stylist)
                  .WithMany(s => s.Appointments)
                  .HasForeignKey(a => a.StylistId)
                  .OnDelete(DeleteBehavior.Restrict);

            // An index makes "find this stylist's bookings on this date" fast.
            entity.HasIndex(a => new { a.StylistId, a.AppointmentDate });
        });
    }
}
