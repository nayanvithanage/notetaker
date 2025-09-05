using Microsoft.EntityFrameworkCore;
using Notetaker.Api.Models;

namespace Notetaker.Api.Data;

public class NotetakerDbContext : DbContext
{
    public NotetakerDbContext(DbContextOptions<NotetakerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserToken> UserTokens { get; set; }
    public DbSet<GoogleCalendarAccount> GoogleCalendarAccounts { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<MeetingTranscript> MeetingTranscripts { get; set; }
    public DbSet<Automation> Automations { get; set; }
    public DbSet<GeneratedContent> GeneratedContents { get; set; }
    public DbSet<SocialAccount> SocialAccounts { get; set; }
    public DbSet<SocialPost> SocialPosts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.PictureUrl).HasMaxLength(500);
            entity.Property(e => e.AuthProvider).HasMaxLength(50).HasDefaultValue("google");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // UserToken configuration
        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(e => e.UserTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AccessToken).IsRequired();
            entity.Property(e => e.Scopes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // GoogleCalendarAccount configuration
        modelBuilder.Entity<GoogleCalendarAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(e => e.GoogleCalendarAccounts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.AccountEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SyncState).HasMaxLength(50).HasDefaultValue("pending");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // CalendarEvent configuration
        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(e => e.CalendarEvents)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.GoogleCalendarAccount)
                .WithMany(e => e.CalendarEvents)
                .HasForeignKey(e => e.GoogleCalendarAccountId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.ExternalEventId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Platform).HasMaxLength(50).HasDefaultValue("unknown");
            entity.Property(e => e.JoinUrl).HasMaxLength(1000);
            entity.Property(e => e.NotetakerEnabled).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Meeting configuration
        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(e => e.Meetings)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CalendarEvent)
                .WithMany(e => e.Meetings)
                .HasForeignKey(e => e.CalendarEventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.RecallBotId).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("scheduled");
            entity.Property(e => e.Platform).HasMaxLength(50).HasDefaultValue("unknown");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // MeetingTranscript configuration
        modelBuilder.Entity<MeetingTranscript>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Meeting)
                .WithMany(e => e.MeetingTranscripts)
                .HasForeignKey(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Source).HasMaxLength(50).HasDefaultValue("recall");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Automation configuration
        modelBuilder.Entity<Automation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(e => e.Automations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Platform).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.ExampleText).HasMaxLength(2000);
            entity.Property(e => e.Enabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // GeneratedContent configuration
        modelBuilder.Entity<GeneratedContent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Meeting)
                .WithMany(e => e.GeneratedContents)
                .HasForeignKey(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Automation)
                .WithMany(e => e.GeneratedContents)
                .HasForeignKey(e => e.AutomationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.Prompt).HasMaxLength(2000);
            entity.Property(e => e.OutputText).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // SocialAccount configuration
        modelBuilder.Entity<SocialAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(e => e.SocialAccounts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AccountId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.SelectedPageId).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // SocialPost configuration
        modelBuilder.Entity<SocialPost>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Meeting)
                .WithMany(e => e.SocialPosts)
                .HasForeignKey(e => e.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.SocialAccount)
                .WithMany(e => e.SocialPosts)
                .HasForeignKey(e => e.SocialAccountId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TargetId).HasMaxLength(255);
            entity.Property(e => e.PostText).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("draft");
            entity.Property(e => e.ExternalPostId).HasMaxLength(255);
            entity.Property(e => e.Error).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}


