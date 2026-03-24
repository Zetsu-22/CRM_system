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
    public DbSet<AuthUserOrganization> UserOrganizations => Set<AuthUserOrganization>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
            b.Property(x => x.FullName).HasMaxLength(256);
            b.Property(x => x.Phone).HasMaxLength(32);
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
                .WithMany(x => x.UserRoles)
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

        modelBuilder.Entity<AuthUserOrganization>(b =>
        {
            b.ToTable("user_organizations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.UserId, x.OrganizationId }).IsUnique();
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.User)
                .WithMany(x => x.OrganizationAccess)
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("refresh_tokens");
            b.HasKey(x => x.Id);
            b.Property(x => x.Token).HasMaxLength(512);
            b.HasIndex(x => x.Token).IsUnique();
            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
        });
    }
}

/// <summary>
/// Пользователь системы. CompanyId — компания, к которой он относится.
/// Флаги IsAccess* — прямые права (аналог EmployeeProfile во Flutter).
/// IsCompanyOwner = true даёт полный доступ ко всем данным компании.
/// </summary>
public sealed class AuthUser : AuditableEntity
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>ID компании (ссылка на crm_main.companies.Id). Null = не привязан к компании.</summary>
    public Guid? CompanyId { get; set; }

    // ── Уровни доступа ──────────────────────────────────────────────────────
    /// <summary>Владелец компании — полный доступ ко всем данным и управлению.</summary>
    public bool IsCompanyOwner { get; set; }

    /// <summary>Базовый/административный профиль — может управлять пользователями.</summary>
    public bool IsBasicProfile { get; set; }

    // ── Права доступа к модулям (точное совпадение с Flutter EmployeeProfile) ─
    /// <summary>Право просматривать и редактировать заказы, менять статусы.</summary>
    public bool IsAccessOrdersEdit { get; set; }

    /// <summary>Роль «сборщик» (picker) — назначается на сборку заказа.</summary>
    public bool IsOrderPicker { get; set; }

    /// <summary>Роль «экспедитор» — назначается на доставку, может редактировать статусы заказов.</summary>
    public bool IsExpeditor { get; set; }

    /// <summary>Роль «водитель» — только просмотр заказов.</summary>
    public bool IsDriver { get; set; }

    /// <summary>Право просматривать контрагентов.</summary>
    public bool IsAccessContractors { get; set; }

    /// <summary>Право редактировать контрагентов.</summary>
    public bool IsAccessContractorsEdit { get; set; }

    /// <summary>Право редактировать скидки контрагентов.</summary>
    public bool IsAccessContractorDiscountEdit { get; set; }

    /// <summary>Право просматривать номенклатуру (товары/услуги).</summary>
    public bool IsAccessNomenclature { get; set; }

    /// <summary>Право редактировать номенклатуру.</summary>
    public bool IsAccessNomenclatureEdit { get; set; }

    /// <summary>Право просматривать финансовый журнал (счета, платежи, УПД).</summary>
    public bool IsAccessFinancialJournal { get; set; }

    /// <summary>Право работать со складами и остатками.</summary>
    public bool IsAccessWarehouses { get; set; }

    // ── Навигация ────────────────────────────────────────────────────────────
    public ICollection<AuthUserRole> UserRoles { get; set; } = new List<AuthUserRole>();
    public ICollection<AuthUserOrganization> OrganizationAccess { get; set; } = new List<AuthUserOrganization>();
}

/// <summary>Доступ пользователя к конкретной организации внутри компании.</summary>
public sealed class AuthUserOrganization : AuditableEntity
{
    public Guid UserId { get; set; }
    public AuthUser? User { get; set; }

    /// <summary>ID организации из crm_main.organizations.</summary>
    public Guid OrganizationId { get; set; }
}

/// <summary>Refresh-токен для продления сессии без повторного ввода пароля.</summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AuthUser? User { get; set; }
    public required string Token { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AuthRole : AuditableEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }

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
