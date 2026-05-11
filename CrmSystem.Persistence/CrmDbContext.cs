using CrmSystem.Domain;
using Microsoft.EntityFrameworkCore;

namespace CrmSystem.Persistence;

public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Counterparty> Counterparties => Set<Counterparty>();
    public DbSet<CounterpartyAddress> CounterpartyAddresses => Set<CounterpartyAddress>();
    public DbSet<CounterpartyContact> CounterpartyContacts => Set<CounterpartyContact>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<SalesOrder> Orders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> OrderLines => Set<SalesOrderLine>();

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    public DbSet<Upd> Upds => Set<Upd>();
    public DbSet<UpdLine> UpdLines => Set<UpdLine>();

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCompany(modelBuilder);
        ConfigureCounterparty(modelBuilder);
        ConfigureItem(modelBuilder);
        ConfigureWarehouse(modelBuilder);
        ConfigureStock(modelBuilder);
        ConfigureOrders(modelBuilder);
        ConfigureInvoices(modelBuilder);
        ConfigureUpds(modelBuilder);
        ConfigurePayments(modelBuilder);
    }

    private static void ConfigureXminConcurrency<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> b)
        where TEntity : AuditableEntity
    {
        b.Property(x => x.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }

    private static void ConfigureCompany(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(b =>
        {
            b.ToTable("companies");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(256);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Organization>(b =>
        {
            b.ToTable("organizations");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(256);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.Company)
                .WithMany(x => x.Organizations)
                .HasForeignKey(x => x.CompanyId);
        });
    }

    private static void ConfigureCounterparty(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Counterparty>(b =>
        {
            b.ToTable("counterparties");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(256);
            b.HasIndex(x => x.CompanyId);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<CounterpartyAddress>(b =>
        {
            b.ToTable("counterparty_addresses");
            b.HasKey(x => x.Id);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.Counterparty)
                .WithMany(x => x.Addresses)
                .HasForeignKey(x => x.CounterpartyId);
        });

        modelBuilder.Entity<CounterpartyContact>(b =>
        {
            b.ToTable("counterparty_contacts");
            b.HasKey(x => x.Id);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.Counterparty)
                .WithMany(x => x.Contacts)
                .HasForeignKey(x => x.CounterpartyId);
        });
    }

    private static void ConfigureItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(b =>
        {
            b.ToTable("items");
            b.HasKey(x => x.Id);
            b.Property(x => x.Sku).HasMaxLength(64);
            b.Property(x => x.Name).HasMaxLength(256);
            b.Property(x => x.Unit).HasMaxLength(32);
            b.HasIndex(x => new { x.CompanyId, x.Sku }).IsUnique();
            b.HasIndex(x => x.CompanyId);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);
        });
    }

    private static void ConfigureWarehouse(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Warehouse>(b =>
        {
            b.ToTable("warehouses");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(256);
            b.HasIndex(x => x.CompanyId);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);
        });
    }

    private static void ConfigureStock(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockBalance>(b =>
        {
            b.ToTable("stock_balances");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.WarehouseId, x.ItemId }).IsUnique();
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId);

            b.HasOne(x => x.Item)
                .WithMany()
                .HasForeignKey(x => x.ItemId);
        });

        modelBuilder.Entity<StockMovement>(b =>
        {
            b.ToTable("stock_movements");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Date);
            b.HasIndex(x => new { x.SourceType, x.SourceId });
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);
        });
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesOrder>(b =>
        {
            b.ToTable("orders");
            b.HasKey(x => x.Id);
            b.Property(x => x.Number).HasMaxLength(32);
            b.HasIndex(x => x.Number);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId);

            b.HasOne(x => x.Counterparty)
                .WithMany()
                .HasForeignKey(x => x.CounterpartyId);
        });

        modelBuilder.Entity<SalesOrderLine>(b =>
        {
            b.ToTable("order_lines");
            b.HasKey(x => x.Id);
            b.Property(x => x.NameSnapshot).HasMaxLength(256);
            b.Ignore(x => x.RowVersion);
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.Order)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.OrderId);

            b.HasOne(x => x.Item)
                .WithMany()
                .HasForeignKey(x => x.ItemId);

            b.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId);
        });
    }

    private static void ConfigureInvoices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(b =>
        {
            b.ToTable("invoices");
            b.HasKey(x => x.Id);
            b.Property(x => x.Number).HasMaxLength(32);
            b.HasIndex(x => x.Number);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<InvoiceLine>(b =>
        {
            b.ToTable("invoice_lines");
            b.HasKey(x => x.Id);
            b.Property(x => x.NameSnapshot).HasMaxLength(256);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.Invoice)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.InvoiceId);
        });
    }

    private static void ConfigureUpds(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Upd>(b =>
        {
            b.ToTable("upds");
            b.HasKey(x => x.Id);
            b.Property(x => x.Number).HasMaxLength(32);
            b.HasIndex(x => x.Number);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<UpdLine>(b =>
        {
            b.ToTable("upd_lines");
            b.HasKey(x => x.Id);
            b.Property(x => x.NameSnapshot).HasMaxLength(256);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.Upd)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.UpdId);
        });
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(b =>
        {
            b.ToTable("payments");
            b.HasKey(x => x.Id);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<PaymentAllocation>(b =>
        {
            b.ToTable("payment_allocations");
            b.HasKey(x => x.Id);
            ConfigureXminConcurrency(b);
            b.HasQueryFilter(x => !x.IsDeleted);

            b.HasOne(x => x.Payment)
                .WithMany(x => x.Allocations)
                .HasForeignKey(x => x.PaymentId);

            b.HasOne(x => x.Invoice)
                .WithMany()
                .HasForeignKey(x => x.InvoiceId);
        });
    }
}
