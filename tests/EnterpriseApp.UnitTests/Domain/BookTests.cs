using EnterpriseApp.Domain.Entities;
using EnterpriseApp.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace EnterpriseApp.UnitTests.Domain;

public class BookTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldInstantiateBookCorrectly()
    {
        // Arrange & Act
        var book = new Book("Clean Architecture", "Robert C. Martin", "978-0134494166", 2017);

        // Assert
        book.Title.Should().Be("Clean Architecture");
        book.Author.Should().Be("Robert C. Martin");
        book.Isbn.Should().Be("978-0134494166");
        book.PublicationYear.Should().Be(2017);
        book.Id.Should().NotBeEmpty();
        book.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTitle_ShouldThrowDomainException(string? invalidTitle)
    {
        // Act
        Action act = () => new Book(invalidTitle!, "Martin Fowler", "978-0321127426", 2002);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Book title cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidAuthor_ShouldThrowDomainException(string? invalidAuthor)
    {
        // Act
        Action act = () => new Book("Patterns of Enterprise Application Architecture", invalidAuthor!, "978-0321127426", 2002);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Book author cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidIsbn_ShouldThrowDomainException(string? invalidIsbn)
    {
        // Act
        Action act = () => new Book("Refactoring", "Martin Fowler", invalidIsbn!, 1999);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Book ISBN cannot be empty*");
    }

    [Theory]
    [InlineData(1400)] // Before Gutenberg printing press (1440)
    [InlineData(2100)] // Far in future
    public void Constructor_WithInvalidPublicationYear_ShouldThrowDomainException(int invalidYear)
    {
        // Act
        Action act = () => new Book("Domain-Driven Design", "Eric Evans", "978-0321125217", invalidYear);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Publication year must be between 1440 and*");
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateBookProperties()
    {
        // Arrange
        var book = new Book("Original Title", "Original Author", "978-0000000000", 2020);

        // Act
        book.UpdateDetails("Updated Title", "Updated Author", "978-1111111111", 2023);

        // Assert
        book.Title.Should().Be("Updated Title");
        book.Author.Should().Be("Updated Author");
        book.Isbn.Should().Be("978-1111111111");
        book.PublicationYear.Should().Be(2023);
    }
}
