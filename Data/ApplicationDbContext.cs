using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TripAnalyzer.Models;

namespace TripAnalyzer.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TransportOption> TransportOptions { get; set; } = null!;
        public DbSet<Trip> Trips { get; set; } = null!;
        public DbSet<TripAnalysis> TripAnalyses { get; set; } = null!;
        public DbSet<SavedTrip> SavedTrips { get; set; } = null!;
        public DbSet<Enquiry> Enquiries { get; set; } = null!;
        public DbSet<ChatbotFAQ> ChatbotFAQs { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<District> Districts { get; set; } = null!;
        public DbSet<FareConfiguration> FareConfigurations { get; set; } = null!;
        public DbSet<City> Cities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // District unique constraint
            builder.Entity<District>()
                .HasIndex(d => new { d.StateCode, d.DistrictCode })
                .IsUnique();

            // City relationships & constraints
            builder.Entity<City>()
                .HasIndex(c => new { c.DistrictId, c.Name })
                .IsUnique();

            builder.Entity<City>()
                .HasOne(c => c.District)
                .WithMany()
                .HasForeignKey(c => c.DistrictId)
                .OnDelete(DeleteBehavior.Cascade);

            // TransportOption Indexes
            builder.Entity<TransportOption>()
                .HasIndex(t => new { t.Source, t.Destination });

            // Trip Relationships
            builder.Entity<Trip>()
                .HasOne(t => t.User)
                .WithMany(u => u.Trips)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // TripAnalysis Relationships
            builder.Entity<TripAnalysis>()
                .HasOne(ta => ta.Trip)
                .WithMany(t => t.Analyses)
                .HasForeignKey(ta => ta.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TripAnalysis>()
                .HasOne(ta => ta.TransportOption)
                .WithMany()
                .HasForeignKey(ta => ta.TransportOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // SavedTrip Relationships
            builder.Entity<SavedTrip>()
                .HasOne(st => st.User)
                .WithMany(u => u.SavedTrips)
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SavedTrip>()
                .HasOne(st => st.Trip)
                .WithMany(t => t.SavedTrips)
                .HasForeignKey(st => st.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            // Enquiry Relationships
            builder.Entity<Enquiry>()
                .HasOne(e => e.User)
                .WithMany(u => u.Enquiries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Notification Relationships
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
