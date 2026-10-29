using EnterpriseApp.Application.DTOs.Library;
using EnterpriseApp.Application.Interfaces.Repositories;
using EnterpriseApp.Domain.Entities;
using EnterpriseApp.Domain.Exceptions;
using FluentValidation;
using ValidationException = EnterpriseApp.Domain.Exceptions.ValidationException;

namespace EnterpriseApp.Application.Services;

public interface IBookService
{
    Task<IReadOnlyList<BookDto>> GetAllBooksAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BookDto>> SearchBooksAsync(string? searchTerm, CancellationToken ct = default);
    Task<BookDto?> GetBookByIdAsync(Guid id, CancellationToken ct = default);
    Task<BookDto> CreateBookAsync(CreateBookRequest request, CancellationToken ct = default);
    Task<bool> DeleteBookAsync(Guid id, CancellationToken ct = default);
}

public class BookService : IBookService
{
    private readonly IBookRepository _repository;
    private readonly IValidator<CreateBookRequest> _validator;

    public BookService(IBookRepository repository, IValidator<CreateBookRequest> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<IReadOnlyList<BookDto>> GetAllBooksAsync(CancellationToken ct = default)
    {
        var books = await _repository.GetAllAsync(ct);
        return books.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<BookDto>> SearchBooksAsync(string? searchTerm, CancellationToken ct = default)
    {
        var books = await _repository.SearchAsync(searchTerm, ct);
        return books.Select(MapToDto).ToList();
    }

    public async Task<BookDto?> GetBookByIdAsync(Guid id, CancellationToken ct = default)
    {
        var book = await _repository.GetByIdAsync(id, ct);
        return book is null ? null : MapToDto(book);
    }

    public async Task<BookDto> CreateBookAsync(CreateBookRequest request, CancellationToken ct = default)
    {
        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var book = new Book(request.Title, request.Author, request.Isbn, request.PublicationYear);
        var created = await _repository.AddAsync(book, ct);
        return MapToDto(created);
    }

    public async Task<bool> DeleteBookAsync(Guid id, CancellationToken ct = default)
    {
        return await _repository.DeleteAsync(id, ct);
    }

    private static BookDto MapToDto(Book book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Author = book.Author,
        Isbn = book.Isbn,
        PublicationYear = book.PublicationYear,
        CreatedAtUtc = book.CreatedAtUtc
    };
}
