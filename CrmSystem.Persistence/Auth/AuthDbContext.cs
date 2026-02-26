using CrmSystem.Domain;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Persistence.Auth;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<AuthUser> Users => Set<AuthUser>();
    public DbSet<AuthRole> Roles => Set<AuthRole>();
    public DbSet<AuthPermission> Permissions => Set<AuthPermission>();
    public DbSet<AuthUserRole> UserRoles => Set<AuthUserRole>();
    public DbSet<AuthRolePermission> RolePermissions => Set<AuthRolePermission>();

    private static void ConfigureXminConcurrency<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> b)
        where TEntity : AuditableEntity
    {
        b.Property(x => x.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuthUser>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.PasswordHash).HasMaxLength(512);
            b.HasIndex(x => x.Email).IsUnique();
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<AuthRole>(b =>
        {
            b.ToTable("roles");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(128);
            b.HasIndex(x => x.Name).IsUnique();
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<AuthPermission>(b =>
        {
            b.ToTable("permissions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(128);
            b.HasIndex(x => x.Code).IsUnique();
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<AuthUserRole>(b =>
        {
            b.ToTable("user_roles");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.User)
                .WithMany(x => x.Roles)
                .HasForeignKey(x => x.UserId);

            b.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<AuthRolePermission>(b =>
        {
            b.ToTable("role_permissions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.Role)
                .WithMany(x => x.Permissions)
                .HasForeignKey(x => x.RoleId);

            b.HasOne(x => x.Permission)
                .WithMany(x => x.Roles)
                .HasForeignKey(x => x.PermissionId);
        });
    }
}

public sealed class AuthUser : AuditableEntity
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<AuthUserRole> Roles { get; set; } = new List<AuthUserRole>();
}

public sealed class AuthRole : AuditableEntity
{
    public required string Name { get; set; }

    public ICollection<AuthUserRole> Users { get; set; } = new List<AuthUserRole>();
    public ICollection<AuthRolePermission> Permissions { get; set; } = new List<AuthRolePermission>();
}

public sealed class AuthPermission : AuditableEntity
{
    public required string Code { get; set; }
    public string? Description { get; set; }

    public ICollection<AuthRolePermission> Roles { get; set; } = new List<AuthRolePermission>();
}

public sealed class AuthUserRole : AuditableEntity
{
    public Guid UserId { get; set; }
    public AuthUser? User { get; set; }

    public Guid RoleId { get; set; }
    public AuthRole? Role { get; set; }
}

public sealed class AuthRolePermission : AuditableEntity
{
    public Guid RoleId { get; set; }
    public AuthRole? Role { get; set; }

    public Guid PermissionId { get; set; }
    public AuthPermission? Permission { get; set; }
}
