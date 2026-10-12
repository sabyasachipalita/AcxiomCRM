using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Unique indexes for Customer
        builder.Entity<Customer>()
            .HasIndex(c => c.CustomerCode)
            .IsUnique();

        builder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        builder.Entity<Customer>()
            .HasIndex(c => c.Phone)
            .IsUnique();

        // Unique index for Lead
        builder.Entity<Lead>()
            .HasIndex(l => l.LeadCode)
            .IsUnique();

        // Lead conversions relationships
        builder.Entity<Lead>()
            .HasOne(l => l.ConvertedCustomer)
            .WithMany()
            .HasForeignKey(l => l.ConvertedCustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Lead>()
            .HasOne(l => l.ConvertedOpportunity)
            .WithMany()
            .HasForeignKey(l => l.ConvertedOpportunityId)
            .OnDelete(DeleteBehavior.SetNull);

        // Customer relationships
        builder.Entity<Customer>()
            .HasOne(c => c.Owner)
            .WithMany(u => u.AssignedCustomers)
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Lead relationships
        builder.Entity<Lead>()
            .HasOne(l => l.Owner)
            .WithMany(u => u.AssignedLeads)
            .HasForeignKey(l => l.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        // Opportunity relationships
        builder.Entity<Opportunity>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Opportunities)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Opportunity>()
            .HasOne(o => o.Lead)
            .WithMany(l => l.Opportunities)
            .HasForeignKey(o => o.LeadId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Opportunity>()
            .HasOne(o => o.Owner)
            .WithMany(u => u.AssignedOpportunities)
            .HasForeignKey(o => o.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        // FollowUp relationships
        builder.Entity<FollowUp>()
            .HasOne(f => f.Customer)
            .WithMany(c => c.FollowUps)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FollowUp>()
            .HasOne(f => f.Lead)
            .WithMany(l => l.FollowUps)
            .HasForeignKey(f => f.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FollowUp>()
            .HasOne(f => f.Opportunity)
            .WithMany(o => o.FollowUps)
            .HasForeignKey(f => f.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FollowUp>()
            .HasOne(f => f.Owner)
            .WithMany(u => u.AssignedFollowUps)
            .HasForeignKey(f => f.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        // Activity relationships
        builder.Entity<Activity>()
            .HasOne(a => a.Customer)
            .WithMany(c => c.Activities)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Activity>()
            .HasOne(a => a.Lead)
            .WithMany(l => l.Activities)
            .HasForeignKey(a => a.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Activity>()
            .HasOne(a => a.Owner)
            .WithMany(u => u.AssignedActivities)
            .HasForeignKey(a => a.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
