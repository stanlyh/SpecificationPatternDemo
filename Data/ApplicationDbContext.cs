using Microsoft.EntityFrameworkCore;

namespace SpecificationPatternDemo;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Post> Posts { get; set; } = null!;
    public DbSet<Like> Likes { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasMany(p => p.Likes).WithOne().HasForeignKey(l => l.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(p => p.Comments).WithOne().HasForeignKey(c => c.PostId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Like>(entity => { entity.HasKey(l => l.Id); });
        modelBuilder.Entity<Comment>(entity => { entity.HasKey(c => c.Id); });
        modelBuilder.Entity<RefreshToken>(entity => { entity.HasKey(r => r.Id); });
    }
}
