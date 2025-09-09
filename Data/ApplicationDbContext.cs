using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_Requirements.Models;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<CustomField> CustomFields { get; set; }
    public DbSet<CustomIdFormat> CustomIdFormats { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<InventoryTag> InventoryTags { get; set; }
    public DbSet<InventoryAccess> InventoryAccesses { get; set; }
    public DbSet<InventoryAttachment> InventoryAttachments { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.Category)
            .WithMany()
            .HasForeignKey(i => i.CategoryId);

        modelBuilder.Entity<InventoryTag>()
            .HasKey(it => new { it.InventoryId, it.TagId });

        modelBuilder.Entity<InventoryAccess>()
            .HasKey(ia => new { ia.InventoryId, ia.UserId });

        modelBuilder.Entity<Item>()
            .HasIndex(i => new { i.InventoryId, i.CustomId })
            .IsUnique();

        // Explicitly ignore JsonDocument properties to prevent EF Core mapping issues
        modelBuilder.Entity<CustomIdFormat>()
            .Ignore(c => c.FormatDefinitionJson);

        modelBuilder.Entity<Item>()
            .Ignore(i => i.FieldDataJson);
    }
}
