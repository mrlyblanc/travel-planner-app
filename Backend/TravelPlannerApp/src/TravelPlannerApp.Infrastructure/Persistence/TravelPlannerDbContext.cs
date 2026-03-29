using Microsoft.EntityFrameworkCore;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Infrastructure.Persistence;

public sealed class TravelPlannerDbContext : DbContext, IUnitOfWork
{
    public TravelPlannerDbContext(DbContextOptions<TravelPlannerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();
    public DbSet<ItineraryMember> ItineraryMembers => Set<ItineraryMember>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventAuditLog> EventAuditLogs => Set<EventAuditLog>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new PreconditionFailedException("The resource was modified by another user. Refresh and retry.");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasMaxLength(80);
            entity.Property(user => user.ConcurrencyToken).HasMaxLength(40).IsConcurrencyToken().IsRequired();
            entity.Property(user => user.Name).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(200).IsRequired();
            entity.Property(user => user.Avatar).HasMaxLength(16).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Itinerary>(entity =>
        {
            entity.ToTable("itineraries");
            entity.HasKey(itinerary => itinerary.Id);
            entity.Property(itinerary => itinerary.Id).HasMaxLength(80);
            entity.Property(itinerary => itinerary.ConcurrencyToken).HasMaxLength(40).IsConcurrencyToken().IsRequired();
            entity.Property(itinerary => itinerary.Title).HasMaxLength(160).IsRequired();
            entity.Property(itinerary => itinerary.Description).HasMaxLength(2000);
            entity.Property(itinerary => itinerary.Destination).HasMaxLength(160).IsRequired();
            entity.Property(itinerary => itinerary.StartDate).HasColumnType("date");
            entity.Property(itinerary => itinerary.EndDate).HasColumnType("date");
            entity.HasOne(itinerary => itinerary.CreatedBy)
                .WithMany(user => user.CreatedItineraries)
                .HasForeignKey(itinerary => itinerary.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ItineraryMember>(entity =>
        {
            entity.ToTable("itinerary_members");
            entity.HasKey(member => new { member.ItineraryId, member.UserId });
            entity.Property(member => member.ItineraryId).HasMaxLength(80);
            entity.Property(member => member.UserId).HasMaxLength(80);
            entity.Property(member => member.AddedByUserId).HasMaxLength(80).IsRequired();
            entity.HasOne(member => member.Itinerary)
                .WithMany(itinerary => itinerary.Members)
                .HasForeignKey(member => member.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(member => member.User)
                .WithMany(user => user.ItineraryMemberships)
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(member => member.AddedByUser)
                .WithMany(user => user.AddedItineraryMembers)
                .HasForeignKey(member => member.AddedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(eventEntity => eventEntity.Id);
            entity.Property(eventEntity => eventEntity.Id).HasMaxLength(80);
            entity.Property(eventEntity => eventEntity.ConcurrencyToken).HasMaxLength(40).IsConcurrencyToken().IsRequired();
            entity.Property(eventEntity => eventEntity.Title).HasMaxLength(160).IsRequired();
            entity.Property(eventEntity => eventEntity.Description).HasMaxLength(4000);
            entity.Property(eventEntity => eventEntity.Category).HasConversion<string>().HasMaxLength(32);
            entity.Property(eventEntity => eventEntity.Color).HasMaxLength(32);
            entity.Property(eventEntity => eventEntity.Timezone).HasMaxLength(120).IsRequired();
            entity.Property(eventEntity => eventEntity.Location).HasMaxLength(200);
            entity.Property(eventEntity => eventEntity.LocationAddress).HasMaxLength(400);
            entity.Property(eventEntity => eventEntity.LocationLat).HasPrecision(9, 6);
            entity.Property(eventEntity => eventEntity.LocationLng).HasPrecision(9, 6);
            entity.Property(eventEntity => eventEntity.Cost).HasPrecision(18, 2);
            entity.HasOne(eventEntity => eventEntity.Itinerary)
                .WithMany(itinerary => itinerary.Events)
                .HasForeignKey(eventEntity => eventEntity.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(eventEntity => eventEntity.CreatedBy)
                .WithMany(user => user.CreatedEvents)
                .HasForeignKey(eventEntity => eventEntity.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(eventEntity => eventEntity.UpdatedBy)
                .WithMany(user => user.UpdatedEvents)
                .HasForeignKey(eventEntity => eventEntity.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Ignore(eventEntity => eventEntity.AuditLogs);
        });

        modelBuilder.Entity<EventAuditLog>(entity =>
        {
            entity.ToTable("event_audit_logs");
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Id).HasMaxLength(80);
            entity.Property(log => log.EventId).HasMaxLength(80).IsRequired();
            entity.Property(log => log.ItineraryId).HasMaxLength(80).IsRequired();
            entity.Property(log => log.Action).HasConversion<string>().HasMaxLength(32);
            entity.Property(log => log.Summary).HasMaxLength(400).IsRequired();
            entity.Property(log => log.SnapshotJson).IsRequired();
            entity.Property(log => log.ChangedByUserId).HasMaxLength(80).IsRequired();
            entity.HasIndex(log => log.EventId);
            entity.HasOne(log => log.Itinerary)
                .WithMany()
                .HasForeignKey(log => log.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(log => log.ChangedByUser)
                .WithMany(user => user.EventAuditLogs)
                .HasForeignKey(log => log.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Ignore(log => log.Event);
        });
    }
}
