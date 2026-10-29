using EnterpriseApp.Domain.Common;
using EnterpriseApp.Domain.Exceptions;

namespace EnterpriseApp.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Category { get; private set; } = "General";
    public DateTime? UpdatedAtUtc { get; private set; }

    private Product() { }

    public Product(string name, string description, decimal price, string category = "General")
    {
        UpdateDetails(name, description, price, category);
    }

    public void UpdateDetails(string name, string description, decimal price, string category = "General")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Product description cannot be empty.");

        if (price < 0)
            throw new DomainException("Product price cannot be negative.");

        Name = name.Trim();
        Description = description.Trim();
        Price = price;
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
