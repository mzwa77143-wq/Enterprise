using EnterpriseApp.Application.DTOs.Customer;
using EnterpriseApp.Application.Interfaces.Repositories;
using EnterpriseApp.Domain.Entities;
using EnterpriseApp.Domain.Exceptions;
using FluentValidation;
using ValidationException = EnterpriseApp.Domain.Exceptions.ValidationException;

namespace EnterpriseApp.Application.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllCustomersAsync(CancellationToken ct = default);
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task<bool> DeleteCustomerAsync(Guid id, CancellationToken ct = default);
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly IValidator<CreateCustomerRequest> _createValidator;
    private readonly IValidator<UpdateCustomerRequest> _updateValidator;

    public CustomerService(
        ICustomerRepository repository,
        IValidator<CreateCustomerRequest> createValidator,
        IValidator<UpdateCustomerRequest> updateValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllCustomersAsync(CancellationToken ct = default)
    {
        var customers = await _repository.GetAllAsync(ct);
        return customers.Select(MapToDto).ToList();
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _repository.GetByIdAsync(id, ct);
        return customer is null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var existing = await _repository.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            throw new DomainException($"Customer with email '{request.Email}' already exists.");

        var customer = new Customer(request.Name, request.Email, request.PhoneNumber);
        var created = await _repository.AddAsync(customer, ct);
        return MapToDto(created);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var customer = await _repository.GetByIdAsync(id, ct);
        if (customer is null)
            throw new EntityNotFoundException(nameof(Customer), id);

        var existingWithEmail = await _repository.GetByEmailAsync(request.Email, ct);
        if (existingWithEmail is not null && existingWithEmail.Id != id)
            throw new DomainException($"Another customer with email '{request.Email}' already exists.");

        customer.UpdateDetails(request.Name, request.Email, request.PhoneNumber);
        await _repository.UpdateAsync(customer, ct);
        return MapToDto(customer);
    }

    public async Task<bool> DeleteCustomerAsync(Guid id, CancellationToken ct = default)
    {
        return await _repository.DeleteAsync(id, ct);
    }

    private static CustomerDto MapToDto(Customer customer) => new()
    {
        CustomerId = customer.Id,
        Name = customer.Name,
        Email = customer.Email,
        PhoneNumber = customer.PhoneNumber,
        CreatedAtUtc = customer.CreatedAtUtc,
        UpdatedAtUtc = customer.UpdatedAtUtc
    };
}
