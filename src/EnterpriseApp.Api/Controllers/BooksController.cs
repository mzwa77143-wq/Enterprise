using EnterpriseApp.Application.DTOs.Library;
using EnterpriseApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchResults = await _bookService.SearchBooksAsync(search, ct);
            return Ok(searchResults);
        }

        var books = await _bookService.GetAllBooksAsync(ct);
        return Ok(books);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<BookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? term, [FromQuery] string? search, CancellationToken ct)
    {
        var query = !string.IsNullOrWhiteSpace(term) ? term : search;
        var books = await _bookService.SearchBooksAsync(query, ct);
        return Ok(books);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var book = await _bookService.GetBookByIdAsync(id, ct);
        if (book is null) return NotFound();
        return Ok(book);
    }

    [HttpPost]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBookRequest request, CancellationToken ct)
    {
        var created = await _bookService.CreateBookAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _bookService.DeleteBookAsync(id, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
