using EnterpriseApp.Domain.Common;
using EnterpriseApp.Domain.Exceptions;

namespace EnterpriseApp.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; private set; }

    private Customer() { }

    public Customer(string name, string email, string phoneNumber)
    {
        UpdateDetails(name, email, phoneNumber);
    }

    public void UpdateDetails(string name, string email, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name cannot be empty.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("A valid customer email is required.");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException("Customer phone number cannot be empty.");

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
