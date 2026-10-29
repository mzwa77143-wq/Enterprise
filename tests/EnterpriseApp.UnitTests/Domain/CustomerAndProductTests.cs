using EnterpriseApp.Domain.Entities;
using EnterpriseApp.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace EnterpriseApp.UnitTests.Domain;

public class CustomerAndProductTests
{
    [Fact]
    public void Customer_Constructor_ShouldNormalizeEmailAndTrimStrings()
    {
        // Arrange & Act
        var customer = new Customer("  John Doe  ", "  John.Doe@Example.com  ", "  +1-555-0199  ");

        // Assert
        customer.Name.Should().Be("John Doe");
        customer.Email.Should().Be("john.doe@example.com");
        customer.PhoneNumber.Should().Be("+1-555-0199");
        customer.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Customer_EmptyName_ShouldThrowDomainException(string? invalidName)
    {
        Action act = () => new Customer(invalidName!, "test@example.com", "555-1234");
        act.Should().Throw<DomainException>().WithMessage("*Customer name cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Customer_InvalidEmail_ShouldThrowDomainException(string? invalidEmail)
    {
        Action act = () => new Customer("Valid Name", invalidEmail!, "555-1234");
        act.Should().Throw<DomainException>().WithMessage("*valid customer email is required*");
    }

    [Fact]
    public void Product_Constructor_ShouldInstantiateValidProduct()
    {
        // Arrange & Act
        var product = new Product("Laptop Pro", "High performance workstation", 1499.99m, "Electronics");

        // Assert
        product.Name.Should().Be("Laptop Pro");
        product.Description.Should().Be("High performance workstation");
        product.Price.Should().Be(1499.99m);
        product.Category.Should().Be("Electronics");
    }

    [Fact]
    public void Product_NegativePrice_ShouldThrowDomainException()
    {
        Action act = () => new Product("Laptop Pro", "High performance workstation", -10.00m);
        act.Should().Throw<DomainException>().WithMessage("*Product price cannot be negative*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Product_EmptyName_ShouldThrowDomainException(string? invalidName)
    {
        Action act = () => new Product(invalidName!, "Description", 10.00m);
        act.Should().Throw<DomainException>().WithMessage("*Product name cannot be empty*");
    }
}
