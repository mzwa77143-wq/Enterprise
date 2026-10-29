using EnterpriseApp.Application.DTOs.Auth;
using EnterpriseApp.Application.DTOs.Customer;
using EnterpriseApp.Application.DTOs.Library;
using EnterpriseApp.Application.DTOs.Product;
using EnterpriseApp.Application.Validators.Auth;
using EnterpriseApp.Application.Validators.Customer;
using EnterpriseApp.Application.Validators.Library;
using EnterpriseApp.Application.Validators.Product;
using FluentAssertions;
using Xunit;

namespace EnterpriseApp.UnitTests.Application.Validators;

public class ValidatorTests
{
    private readonly CreateBookRequestValidator _bookValidator = new();
    private readonly CreateCustomerRequestValidator _customerValidator = new();
    private readonly CreateProductRequestValidator _productValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();
    private readonly RegisterRequestValidator _registerValidator = new();

    [Fact]
    public void CreateBookRequestValidator_WithValidInput_ShouldBeValid()
    {
        var request = new CreateBookRequest
        {
            Title = "Designing Data-Intensive Applications",
            Author = "Martin Kleppmann",
            Isbn = "978-1449373320",
            PublicationYear = 2017
        };

        var result = _bookValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Author", "978-1449373320", 2020)]
    [InlineData("Title", "", "978-1449373320", 2020)]
    [InlineData("Title", "Author", "invalid-isbn-123", 2020)]
    [InlineData("Title", "Author", "978-1449373320", 1200)]
    public void CreateBookRequestValidator_WithInvalidInputs_ShouldFailValidation(string title, string author, string isbn, int year)
    {
        var request = new CreateBookRequest
        {
            Title = title,
            Author = author,
            Isbn = isbn,
            PublicationYear = year
        };

        var result = _bookValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateCustomerRequestValidator_WithValidInput_ShouldBeValid()
    {
        var request = new CreateCustomerRequest
        {
            Name = "Alice Wonderland",
            Email = "alice@example.org",
            PhoneNumber = "+1-555-888-9999"
        };

        var result = _customerValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "alice@example.com", "+1-555-0100")]
    [InlineData("Alice", "not-valid-email", "+1-555-0100")]
    [InlineData("Alice", "alice@example.com", "12")] // Too short for phone regex
    public void CreateCustomerRequestValidator_WithInvalidInput_ShouldFailValidation(string name, string email, string phone)
    {
        var request = new CreateCustomerRequest
        {
            Name = name,
            Email = email,
            PhoneNumber = phone
        };

        var result = _customerValidator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateProductRequestValidator_WithValidInput_ShouldBeValid()
    {
        var request = new CreateProductRequest
        {
            Name = "Mechanical Keyboard",
            Description = "RGB backlit wireless mechanical keyboard",
            Price = 99.99m,
            Category = "Accessories"
        };

        var result = _productValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("A", "Valid description here", 10.0, "Tech")] // Name too short (<2)
    [InlineData("Valid Name", "Tiny", 10.0, "Tech")] // Desc too short (<5)
    [InlineData("Valid Name", "Valid description here", -5.0, "Tech")] // Price negative
    [InlineData("Valid Name", "Valid description here", 0.0, "Tech")] // Price < 0.01
    public void CreateProductRequestValidator_WithInvalidInput_ShouldFailValidation(string name, string desc, decimal price, string category)
    {
        var request = new CreateProductRequest
        {
            Name = name,
            Description = desc,
            Price = price,
            Category = category
        };

        var result = _productValidator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LoginRequestValidator_WithMissingCredentials_ShouldFail()
    {
        var result = _loginValidator.Validate(new LoginRequest { Username = "", Password = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("ad", "admin@example.com", "password123", "Admin")] // username < 3
    [InlineData("admin", "invalid-email", "password123", "Admin")] // invalid email
    [InlineData("admin", "admin@example.com", "12345", "Admin")] // password < 6
    [InlineData("admin", "admin@example.com", "password123", "SuperAdmin")] // invalid role
    public void RegisterRequestValidator_WithInvalidInputs_ShouldFail(string username, string email, string password, string role)
    {
        var request = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password,
            Role = role
        };

        var result = _registerValidator.Validate(request);
        result.IsValid.Should().BeFalse();
    }
}
