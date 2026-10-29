using EnterpriseApp.Application.DTOs.Auth;
using EnterpriseApp.Application.DTOs.Customer;
using EnterpriseApp.Application.DTOs.Library;
using EnterpriseApp.Application.DTOs.Product;
using EnterpriseApp.Application.Interfaces.Repositories;
using EnterpriseApp.Application.Interfaces.Security;
using EnterpriseApp.Application.Services;
using EnterpriseApp.Application.Validators.Auth;
using EnterpriseApp.Application.Validators.Customer;
using EnterpriseApp.Application.Validators.Library;
using EnterpriseApp.Application.Validators.Product;
using EnterpriseApp.Domain.Entities;
using EnterpriseApp.Domain.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace EnterpriseApp.UnitTests.Application.Services;

public class ServiceTests
{
    [Fact]
    public async Task BookService_CreateBookAsync_ValidRequest_ShouldAddAndReturnDto()
    {
        // Arrange
        var mockRepo = new Mock<IBookRepository>();
        var validator = new CreateBookRequestValidator();
        var service = new BookService(mockRepo.Object, validator);

        var request = new CreateBookRequest
        {
            Title = "Refactoring",
            Author = "Martin Fowler",
            Isbn = "978-0201485677",
            PublicationYear = 1999
        };

        mockRepo.Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book b, CancellationToken _) => b);

        // Act
        var result = await service.CreateBookAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Refactoring");
        result.Author.Should().Be("Martin Fowler");
        result.Isbn.Should().Be("978-0201485677");
        mockRepo.Verify(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookService_DeleteBookAsync_ShouldDelegateToRepository()
    {
        // Arrange
        var mockRepo = new Mock<IBookRepository>();
        var service = new BookService(mockRepo.Object, new CreateBookRequestValidator());
        var bookId = Guid.NewGuid();

        mockRepo.Setup(r => r.DeleteAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await service.DeleteBookAsync(bookId);

        // Assert
        result.Should().BeTrue();
        mockRepo.Verify(r => r.DeleteAsync(bookId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookService_SearchBooksAsync_ShouldDelegateToRepository()
    {
        // Arrange
        var mockRepo = new Mock<IBookRepository>();
        var service = new BookService(mockRepo.Object, new CreateBookRequestValidator());
        var books = new List<Book>
        {
            new Book("Clean Code", "Robert C. Martin", "978-0132350884", 2008),
            new Book("Clean Architecture", "Robert C. Martin", "978-0134494166", 2017)
        };

        mockRepo.Setup(r => r.SearchAsync("clean", It.IsAny<CancellationToken>()))
            .ReturnsAsync(books);

        // Act
        var result = await service.SearchBooksAsync("clean");

        // Assert
        result.Should().HaveCount(2);
        result.First().Title.Should().Be("Clean Code");
        mockRepo.Verify(r => r.SearchAsync("clean", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CustomerService_CreateCustomerAsync_DuplicateEmail_ShouldThrowDomainException()
    {
        // Arrange
        var mockRepo = new Mock<ICustomerRepository>();
        var createValidator = new CreateCustomerRequestValidator();
        var updateValidator = new UpdateCustomerRequestValidator();
        var service = new CustomerService(mockRepo.Object, createValidator, updateValidator);

        var request = new CreateCustomerRequest
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "+1-555-0100"
        };

        var existingCustomer = new Customer("John Original", "john.doe@example.com", "+1-555-0100");
        mockRepo.Setup(r => r.GetByEmailAsync("john.doe@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        // Act
        Func<Task> act = async () => await service.CreateCustomerAsync(request);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CustomerService_GetCustomerByIdAsync_NonExistent_ShouldReturnNull()
    {
        // Arrange
        var mockRepo = new Mock<ICustomerRepository>();
        var service = new CustomerService(mockRepo.Object, new CreateCustomerRequestValidator(), new UpdateCustomerRequestValidator());

        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await service.GetCustomerByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ProductService_CreateProductAsync_ValidRequest_ShouldReturnProductDto()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        var service = new ProductService(mockRepo.Object, new CreateProductRequestValidator(), new UpdateProductRequestValidator());

        var request = new CreateProductRequest
        {
            Name = "Ergonomic Chair",
            Description = "Adjustable lumbar support ergonomic chair",
            Price = 299.99m,
            Category = "Furniture"
        };

        mockRepo.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        // Act
        var result = await service.CreateProductAsync(request);

        // Assert
        result.Name.Should().Be("Ergonomic Chair");
        result.Price.Should().Be(299.99m);
        result.Category.Should().Be("Furniture");
        mockRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthService_LoginAsync_ValidCredentials_ShouldReturnAuthResponseWithToken()
    {
        // Arrange
        var mockUserRepo = new Mock<IUserRepository>();
        var mockTokenService = new Mock<ITokenService>();
        var mockHasher = new Mock<IPasswordHasher>();

        var testUser = new User("admin", "admin@example.com", "hashed_pw", "Admin");
        mockUserRepo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUser);
        mockHasher.Setup(h => h.VerifyPassword("Admin@123", "hashed_pw"))
            .Returns(true);
        mockTokenService.Setup(t => t.GenerateToken(testUser))
            .Returns("fake-jwt-token-string");

        var service = new AuthService(
            mockUserRepo.Object,
            mockTokenService.Object,
            mockHasher.Object,
            new LoginRequestValidator(),
            new RegisterRequestValidator());

        // Act
        var response = await service.LoginAsync(new LoginRequest { Username = "admin", Password = "Admin@123" });

        // Assert
        response.Token.Should().Be("fake-jwt-token-string");
        response.Username.Should().Be("admin");
        response.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task AuthService_LoginAsync_InvalidPassword_ShouldThrowDomainException()
    {
        // Arrange
        var mockUserRepo = new Mock<IUserRepository>();
        var mockTokenService = new Mock<ITokenService>();
        var mockHasher = new Mock<IPasswordHasher>();

        var testUser = new User("admin", "admin@example.com", "hashed_pw", "Admin");
        mockUserRepo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(testUser);
        mockHasher.Setup(h => h.VerifyPassword("WrongPassword", "hashed_pw"))
            .Returns(false);

        var service = new AuthService(
            mockUserRepo.Object,
            mockTokenService.Object,
            mockHasher.Object,
            new LoginRequestValidator(),
            new RegisterRequestValidator());

        // Act
        Func<Task> act = async () => await service.LoginAsync(new LoginRequest { Username = "admin", Password = "WrongPassword" });

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Invalid username or password*");
    }
}
