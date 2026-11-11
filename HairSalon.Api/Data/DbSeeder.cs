using HairSalon.Api.Models;
using HairSalon.Shared.Enums;
using HairSalon.Shared.Rules;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HairSalon.Api.Data;

/// <summary>
/// Runs once at start-up (called from Program.cs):
///  1. applies EF Core migrations (creates/upgrades salon.db)
///  2. creates the roles and test accounts
///  3. inserts realistic sample data so every page has something to show.
///
/// Every seed method first checks whether its table already has data, so it is safe to
/// run on every start - and new tables added in later commits get their data too.
/// Dates are calculated from TODAY, so the demo data always looks current.
/// Tip: delete salon.db (and salon.db-shm / salon.db-wal) to start over with fresh data.
/// </summary>
public static class DbSeeder
{
    // The seeder uses fixed opening hours (09-18) so it does not depend on later code.
    private const int OpeningMinute = 9 * 60;
    private const int ClosingMinute = 18 * 60;

    public static async Task InitializeAsync(IServiceProvider services)
    {
        // Services such as the DbContext are "scoped" (one per request). At start-up there
        // is no request, so we create a scope ourselves.
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<SalonDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        if (!db.Database.GetMigrations().Any())
        {
            throw new InvalidOperationException(
                "No EF Core migrations were found. Open Tools > NuGet Package Manager > Package Manager Console, " +
                "select HairSalon.Api as 'Default project' and run:  Add-Migration InitialCreate  - then start the API again.");
        }

        // Creates the database if needed and applies any pending migrations.
        await db.Database.MigrateAsync();

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        await SeedRolesAsync(roleManager);

        var admin = await SeedUserAsync(userManager, "admin@salon.se", "Admin123!", "Sofia", "Lind", "070-111 22 33", Roles.Admin, 400);
        var customers = new List<ApplicationUser>
        {
            // customers[0] = Anna, the account you log in with to test the customer pages.
            await SeedUserAsync(userManager, "anna@salon.se", "Customer123!", "Anna", "Svensson", "070-222 33 44", Roles.Customer, 300),
            await SeedUserAsync(userManager, "erik@salon.se", "Customer123!", "Erik", "Johansson", "070-333 44 55", Roles.Customer, 280),
            await SeedUserAsync(userManager, "maria@salon.se", "Customer123!", "Maria", "Karlsson", "070-444 55 66", Roles.Customer, 250),
            await SeedUserAsync(userManager, "johan@salon.se", "Customer123!", "Johan", "Nilsson", "070-555 66 77", Roles.Customer, 200),
            await SeedUserAsync(userManager, "lina@salon.se", "Customer123!", "Lina", "Berg", "070-666 77 88", Roles.Customer, 120)
        };

        await SeedServicesAsync(db);
        await SeedStylistsAsync(db);
        await SeedAppointmentsAsync(db, customers);

        logger.LogInformation("Database is ready.");
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { Roles.Admin, Roles.Customer })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task<ApplicationUser> SeedUserAsync(
        UserManager<ApplicationUser> userManager, string email, string password,
        string firstName, string lastName, string phone, string role, int registeredDaysAgo)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null) return user;

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phone,
            RegistrationDate = DateTime.Today.AddDays(-registeredDaysAgo)
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not create {email}: " +
                string.Join(", ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private static async Task SeedServicesAsync(SalonDbContext db)
    {
        if (await db.Services.AnyAsync()) return;

        db.Services.AddRange(
            new Service { Name = "Women's Haircut", Category = ServiceCategory.Haircut, Price = 550, DurationMinutes = 60, Description = "Consultation, wash, precision cut and blow-dry." },
            new Service { Name = "Men's Haircut", Category = ServiceCategory.Haircut, Price = 395, DurationMinutes = 30, Description = "Classic or modern cut, finished with styling." },
            new Service { Name = "Children's Haircut", Category = ServiceCategory.Haircut, Price = 250, DurationMinutes = 30, Description = "A relaxed haircut for children up to 12 years." },
            new Service { Name = "Hair Coloring", Category = ServiceCategory.Coloring, Price = 1200, DurationMinutes = 120, Description = "Full colour from roots to ends with a gloss finish." },
            new Service { Name = "Highlights", Category = ServiceCategory.Coloring, Price = 1450, DurationMinutes = 150, Description = "Foil highlights or balayage for a natural sun-kissed look." },
            new Service { Name = "Beard Trim", Category = ServiceCategory.Grooming, Price = 200, DurationMinutes = 30, Description = "Shaping and trimming with hot towel and beard oil." },
            new Service { Name = "Hair Styling", Category = ServiceCategory.Styling, Price = 450, DurationMinutes = 45, Description = "Blow-out, curls or an updo for a special occasion." },
            new Service { Name = "Perm Styling", Category = ServiceCategory.Styling, Price = 1350, DurationMinutes = 120, Description = "Long-lasting waves or curls with a gentle perm." },
            new Service { Name = "Hair Treatment", Category = ServiceCategory.Treatment, Price = 600, DurationMinutes = 45, Description = "Deep-conditioning treatment that repairs dry and damaged hair." },
            new Service { Name = "Bridal Styling", Category = ServiceCategory.Styling, Price = 1800, DurationMinutes = 120, Description = "Trial and wedding-day styling for the bride." },
            new Service { Name = "Keratin Treatment", Category = ServiceCategory.Treatment, Price = 2200, DurationMinutes = 180, Description = "Smoothing treatment - currently not offered.", IsActive = false });

        await db.SaveChangesAsync();
    }

    private static async Task SeedStylistsAsync(SalonDbContext db)
    {
        if (await db.Stylists.AnyAsync()) return;

        db.Stylists.AddRange(
            new Stylist { FirstName = "Emma", LastName = "Lindqvist", Email = "emma@salon.se", PhoneNumber = "070-100 10 01", Specialization = "Hair Cutting & Styling", YearsOfExperience = 12, Bio = "Emma loves precision cuts and has trained in London and Stockholm. Known for listening carefully before picking up the scissors." },
            new Stylist { FirstName = "Oscar", LastName = "Berg", Email = "oscar@salon.se", PhoneNumber = "070-100 10 02", Specialization = "Men's Cuts & Beard Grooming", YearsOfExperience = 8, Bio = "Oscar is our barber specialist - fades, classic cuts and perfectly shaped beards." },
            new Stylist { FirstName = "Sara", LastName = "Ahmed", Email = "sara@salon.se", PhoneNumber = "070-100 10 03", Specialization = "Coloring & Highlights", YearsOfExperience = 10, Bio = "Sara is a certified colour expert who creates natural balayage and bold fashion colours." },
            new Stylist { FirstName = "Julia", LastName = "Nyström", Email = "julia@salon.se", PhoneNumber = "070-100 10 04", Specialization = "Bridal Styling", YearsOfExperience = 6, Bio = "Julia has styled more than 200 brides and makes sure the hair lasts the whole wedding day." },
            new Stylist { FirstName = "Leo", LastName = "Andersson", Email = "leo@salon.se", PhoneNumber = "070-100 10 05", Specialization = "Hair Treatments & Perms", YearsOfExperience = 3, Bio = "Leo focuses on healthy hair: treatments, perms and scalp care. Currently on parental leave.", IsAvailable = false });

        await db.SaveChangesAsync();
    }

    private static async Task SeedAppointmentsAsync(SalonDbContext db, List<ApplicationUser> customers)
    {
        if (await db.Appointments.AnyAsync()) return;

        var services = await db.Services.Where(s => s.IsActive).ToListAsync();
        var stylists = await db.Stylists.ToListAsync();
        var availableStylists = stylists.Where(s => s.IsAvailable).ToList();
        var random = new Random(2026);   // fixed seed = the same "random" data every time
        var today = DateOnly.FromDateTime(DateTime.Today);
        var list = new List<Appointment>();
        Service ServiceNamed(string name) => services.First(s => s.Name == name);
        Stylist StylistNamed(string firstName) => stylists.First(s => s.FirstName == firstName);

        // 1) History: about 45 visits during the last 8 months.
        //    Some customers visit more often than others -> different loyalty tiers.
        var weightedCustomers = new List<ApplicationUser>();
        weightedCustomers.AddRange(Enumerable.Repeat(customers[1], 4)); // Erik  - frequent
        weightedCustomers.AddRange(Enumerable.Repeat(customers[2], 3)); // Maria
        weightedCustomers.AddRange(Enumerable.Repeat(customers[3], 2)); // Johan
        weightedCustomers.Add(customers[4]);                            // Lina  - occasional

        for (int i = 0; i < 45; i++)
        {
            var date = OpenDay(today.AddDays(-random.Next(1, 240)), -1);
            var status = random.Next(100) < 85 ? AppointmentStatus.Completed : AppointmentStatus.Cancelled;
            TryAdd(list, random, date,
                weightedCustomers[random.Next(weightedCustomers.Count)],
                services[random.Next(services.Count)],
                stylists[random.Next(stylists.Count)],
                status);
        }

        // 2) Today and the next two weeks: pending and confirmed bookings.
        var otherCustomers = customers.Skip(1).ToList();
        for (int i = 0; i < 4; i++)
        {
            if (!SalonIsOpen(today)) break;
            TryAdd(list, random, today, otherCustomers[random.Next(otherCustomers.Count)],
                services[random.Next(services.Count)], availableStylists[random.Next(availableStylists.Count)],
                AppointmentStatus.Confirmed);
        }
        for (int i = 0; i < 20; i++)
        {
            var date = OpenDay(today.AddDays(random.Next(1, 15)), +1);
            var status = random.Next(100) < 60 ? AppointmentStatus.Confirmed : AppointmentStatus.Pending;
            TryAdd(list, random, date, otherCustomers[random.Next(otherCustomers.Count)],
                services[random.Next(services.Count)], availableStylists[random.Next(availableStylists.Count)],
                status);
        }

        // 3) Fixed demo data for Anna (anna@salon.se) so every customer feature can be tested:
        //    3 completed visits (= Silver loyalty tier), 1 cancelled, 1 confirmed tomorrow, 1 pending.
        var anna = customers[0];
        TryAdd(list, random, OpenDay(today.AddDays(-100), -1), anna, ServiceNamed("Hair Coloring"), StylistNamed("Sara"), AppointmentStatus.Completed);
        TryAdd(list, random, OpenDay(today.AddDays(-40), -1), anna, ServiceNamed("Highlights"), StylistNamed("Sara"), AppointmentStatus.Completed);
        TryAdd(list, random, OpenDay(today.AddDays(-20), -1), anna, ServiceNamed("Hair Styling"), StylistNamed("Julia"), AppointmentStatus.Cancelled);
        TryAdd(list, random, OpenDay(today.AddDays(-3), -1), anna, ServiceNamed("Women's Haircut"), StylistNamed("Emma"), AppointmentStatus.Completed);
        TryAdd(list, random, OpenDay(today.AddDays(1), +1), anna, ServiceNamed("Women's Haircut"), StylistNamed("Emma"), AppointmentStatus.Confirmed, "Just a trim, please.");
        TryAdd(list, random, OpenDay(today.AddDays(6), +1), anna, ServiceNamed("Hair Treatment"), StylistNamed("Julia"), AppointmentStatus.Pending);

        db.Appointments.AddRange(list);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Adds an appointment at a random free time on the given date. Tries a few times and
    /// gives up if the stylist (or customer) is busy at every time it tried.
    /// </summary>
    private static bool TryAdd(List<Appointment> list, Random random, DateOnly date,
        ApplicationUser customer, Service service, Stylist stylist, AppointmentStatus status, string? notes = null)
    {
        for (int attempt = 0; attempt < 15; attempt++)
        {
            var latestStart = ClosingMinute - service.DurationMinutes;
            var slotCount = (latestStart - OpeningMinute) / 30 + 1;
            var startMinute = OpeningMinute + random.Next(slotCount) * 30;
            var start = new TimeOnly(startMinute / 60, startMinute % 60);
            var end = start.AddMinutes(service.DurationMinutes);

            // Two time ranges overlap when each one starts before the other one ends.
            bool Overlaps(Appointment a) =>
                a.AppointmentDate == date && a.Status != AppointmentStatus.Cancelled &&
                start < a.EndTime && a.StartTime < end;

            if (list.Any(a => a.StylistId == stylist.StylistId && Overlaps(a))) continue;
            if (list.Any(a => a.CustomerId == customer.Id && Overlaps(a))) continue;

            list.Add(new Appointment
            {
                AppointmentDate = date,
                StartTime = start,
                EndTime = end,
                Status = status,
                Notes = notes,
                Price = service.Price,
                CustomerId = customer.Id,
                ServiceId = service.ServiceId,
                StylistId = stylist.StylistId,
                CreatedAt = date.ToDateTime(start).AddDays(-random.Next(2, 15))
            });
            return true;
        }
        return false;
    }

    private static bool SalonIsOpen(DateOnly date) => date.DayOfWeek != DayOfWeek.Sunday;

    /// <summary>Moves a Sunday one day back (-1) or forward (+1).</summary>
    private static DateOnly OpenDay(DateOnly date, int direction) =>
        SalonIsOpen(date) ? date : date.AddDays(direction);
}
