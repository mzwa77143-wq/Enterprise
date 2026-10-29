using EnterpriseApp.Domain.Common;
using EnterpriseApp.Domain.Exceptions;

namespace EnterpriseApp.Domain.Entities;

public class Book : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public string Isbn { get; private set; } = string.Empty;
    public int PublicationYear { get; private set; }

    private Book() { } // EF Core

    public Book(string title, string author, string isbn, int publicationYear)
    {
        UpdateDetails(title, author, isbn, publicationYear);
    }

    public void UpdateDetails(string title, string author, string isbn, int publicationYear)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Book title cannot be empty.");

        if (string.IsNullOrWhiteSpace(author))
            throw new DomainException("Book author cannot be empty.");

        if (string.IsNullOrWhiteSpace(isbn))
            throw new DomainException("Book ISBN cannot be empty.");

        if (publicationYear < 1440 || publicationYear > DateTime.UtcNow.Year + 1)
            throw new DomainException($"Publication year must be between 1440 and {DateTime.UtcNow.Year + 1}.");

        Title = title.Trim();
        Author = author.Trim();
        Isbn = isbn.Trim();
        PublicationYear = publicationYear;
    }
}
