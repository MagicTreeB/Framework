using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using MMO.Core.Entity.Entities;
using MMO.Core.Middlewares.MultiTenancy;
using System.Linq.Expressions;
using MMO.Core.Share;

namespace YourApi.Infra.Data;

/// <summary>
/// Example DbContext implementation with automatic tenant filtering.
/// Copy this pattern to any API that needs multi-tenancy support.
/// </summary>
public class ExampleApplicationDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public ExampleApplicationDbContext(
        DbContextOptions<ExampleApplicationDbContext> options,
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    // Example DbSets
    public DbSet<ExampleProduct> Products => Set<ExampleProduct>();
    public DbSet<ExampleOrder> Orders => Set<ExampleOrder>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExampleApplicationDbContext).Assembly);
        
        // Apply global query filters for multi-tenancy
        ApplyTenantQueryFilters(modelBuilder);
    }
    
    /// <summary>
    /// Applies automatic OrganizationId filtering to all entities
    /// inheriting from OwnerOrganizationEntity.
    /// 
    /// Behavior:
    /// - Host role: No filtering, sees all data across all organizations
    /// - Admin/Staff/Client: Filtered by their OrganizationId
    /// - Unauthenticated: No data returned (OrganizationId is null)
    /// </summary>
    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        // No HttpContext available (e.g., during migrations)
        if (httpContext == null)
            return;
        
        // Get tenant context from middleware
        var tenantContext = httpContext.GetTenantContext();
        
        if (tenantContext == null)
            return;
        
        // Apply filter to all entities inheriting OwnerOrganizationEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Check if entity inherits from OwnerOrganizationEntity
            if (typeof(OwnerOrganizationEntity<>).IsAssignableFrom(entityType.ClrType))
            {
                // Host role: no filter (see all data)
                if (tenantContext.IsHost)
                {
                    // Do not apply any filter for Host users
                    continue;
                }
                
                // Other roles: filter by OrganizationId
                if (tenantContext.OrganizationId.HasValue)
                {
                    // Build expression: entity.OrganizationId == tenantContext.OrganizationId
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(OwnerOrganizationEntity<Guid>.OrganizationId));
                    var filterValue = Expression.Constant(tenantContext.OrganizationId.Value);
                    var equalExpression = Expression.Equal(property, filterValue);
                    var lambdaExpression = Expression.Lambda(equalExpression, parameter);
                    
                    // Apply the query filter
                    entityType.SetQueryFilter(lambdaExpression);
                }
                else
                {
                    // No OrganizationId in claims - filter out all data
                    // This prevents unauthenticated or improperly configured users from seeing any data
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var falseExpression = Expression.Constant(false);
                    var lambdaExpression = Expression.Lambda(falseExpression, parameter);
                    
                    entityType.SetQueryFilter(lambdaExpression);
                }
            }
        }
    }
}

/// <summary>
/// Example Product entity with automatic tenant isolation
/// </summary>
public class ExampleProduct : OwnerOrganizationEntity<Guid>
{    // Private constructor for EF Core
    private ExampleProduct() : base(Guid.NewGuid(), Guid.NewGuid(), "System") { }
    
    // Private constructor for Create factory method
    private ExampleProduct(Guid id, Guid organizationId, string createdBy) : base(id, organizationId, createdBy) { }
        public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    
    public static ExampleProduct Create(
        string name, 
        string description, 
        decimal price, 
        Guid organizationId,
        string? createdBy = null)
    {
        return new ExampleProduct
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            OrganizationId = organizationId,
            IsActive = true,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow,
            CreatedBy = createdBy ?? ShareKey.SystemUser,
            UpdatedBy = createdBy ?? ShareKey.SystemUser
        };
    }
}

/// <summary>
/// Example Order entity with automatic tenant isolation
/// </summary>
public class ExampleOrder : OwnerOrganizationEntity<Guid>
{    // Private constructor for EF Core
    private ExampleOrder() : base(Guid.NewGuid(), Guid.NewGuid(), ShareKey.SystemUser) { }
    
    // Private constructor for Create factory method
    private ExampleOrder(Guid id, Guid organizationId, string createdBy) : base(id, organizationId, createdBy) { }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid ProductId { get; set; }
    
    public static ExampleOrder Create(
        string orderNumber,
        decimal totalAmount,
        Guid productId,
        Guid organizationId,
        string? createdBy = null)
    {
        return new ExampleOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            TotalAmount = totalAmount,
            ProductId = productId,
            OrganizationId = organizationId,
            Status = "Pending",
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow,
            CreatedBy = createdBy ?? "System",
            UpdatedBy = createdBy ?? "System"
        };
    }
}
