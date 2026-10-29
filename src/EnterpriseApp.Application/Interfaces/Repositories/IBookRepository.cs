using EnterpriseApp.Domain.Entities;

namespace EnterpriseApp.Application.Interfaces.Repositories;

public interface IBookRepository
{
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Book>> SearchAsync(string? searchTerm, CancellationToken ct = default);
    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Book> AddAsync(Book book, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
