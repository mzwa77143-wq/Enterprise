using EnterpriseApp.Application.Interfaces.Repositories;
using EnterpriseApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseApp.Infrastructure.Persistence.Repositories;

public class BookRepository : IBookRepository
{
    private readonly EnterpriseDbContext _context;

    public BookRepository(EnterpriseDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Books
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Book>> SearchAsync(string? searchTerm, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllAsync(ct);

        var term = searchTerm.Trim().ToLower();
        return await _context.Books
            .Where(b => b.Title.ToLower().Contains(term)
                     || b.Author.ToLower().Contains(term)
                     || b.Isbn.ToLower().Contains(term))
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Books.FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<Book> AddAsync(Book book, CancellationToken ct = default)
    {
        await _context.Books.AddAsync(book, ct);
        await _context.SaveChangesAsync(ct);
        return book;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (book is null) return false;

        _context.Books.Remove(book);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
