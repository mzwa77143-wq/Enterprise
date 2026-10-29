using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseApp.Application.DTOs.Auth;
using EnterpriseApp.Application.DTOs.Customer;
using EnterpriseApp.Application.DTOs.Factorial;
using EnterpriseApp.Application.DTOs.Library;
using EnterpriseApp.Application.DTOs.Messaging;
using EnterpriseApp.Application.DTOs.Product;
using EnterpriseApp.Application.Interfaces.Services;

namespace EnterpriseApp.BlazorUI.Services;

public class ApiClientService
{
    private readonly HttpClient _http;
    private readonly CustomAuthenticationStateProvider _authStateProvider;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiClientService(HttpClient http, CustomAuthenticationStateProvider authStateProvider)
    {
        _http = http;
        _authStateProvider = authStateProvider;
    }

    private void SetAuthorizationHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_authStateProvider.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authStateProvider.Token);
        }
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request)
    {
        SetAuthorizationHeader(request);
        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API request failed ({response.StatusCode}): {content}", null, response.StatusCode);
        }

        return (await response.Content.ReadFromJsonAsync<T>(_jsonOptions))!;
    }

    private async Task SendAsync(HttpRequestMessage request)
    {
        SetAuthorizationHeader(request);
        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API request failed ({response.StatusCode}): {content}", null, response.StatusCode);
        }
    }

    // --- Authentication ---
    public async Task<AuthResponseDto> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed: {error}");
        }

        var result = (await response.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions))!;
        _authStateProvider.MarkUserAsAuthenticated(result.Token);
        return result;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Registration failed: {error}");
        }

        var result = (await response.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions))!;
        _authStateProvider.MarkUserAsAuthenticated(result.Token);
        return result;
    }

    public void Logout()
    {
        _authStateProvider.MarkUserAsLoggedOut();
    }

    public async Task<UserProfileDto?> GetCurrentUserAsync()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        return await SendAsync<UserProfileDto>(req);
    }

    // --- Module 1: Library Management System ---
    public async Task<IReadOnlyList<BookDto>> GetBooksAsync(string? search = null)
    {
        var url = string.IsNullOrWhiteSpace(search) ? "api/books" : $"api/books?search={Uri.EscapeDataString(search)}";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<IReadOnlyList<BookDto>>(req);
    }

    public async Task<BookDto> GetBookByIdAsync(Guid id)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"api/books/{id}");
        return await SendAsync<BookDto>(req);
    }

    public async Task<BookDto> CreateBookAsync(CreateBookRequest request)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/books")
        {
            Content = JsonContent.Create(request)
        };
        return await SendAsync<BookDto>(req);
    }

    public async Task DeleteBookAsync(Guid id)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/books/{id}");
        await SendAsync(req);
    }

    // --- Module 2: Multi-threaded Factorial Calculator ---
    public async Task<BatchCalculationResultDto> CalculateFactorialsAsync(BatchCalculationRequest request)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/factorial/batch")
        {
            Content = JsonContent.Create(request)
        };
        return await SendAsync<BatchCalculationResultDto>(req);
    }

    // --- Module 3: Azure Service Bus Messaging ---
    public async Task<JsonElement> SendServiceBusMessageAsync(SendMessageRequest request)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/messaging/send")
        {
            Content = JsonContent.Create(request)
        };
        return await SendAsync<JsonElement>(req);
    }

    public async Task<IReadOnlyList<ServiceBusMessageDto>> GetMessageLogsAsync()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "api/messaging/logs");
        return await SendAsync<IReadOnlyList<ServiceBusMessageDto>>(req);
    }

    public async Task<QueueMetricsDto> GetMessageMetricsAsync()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "api/messaging/metrics");
        return await SendAsync<QueueMetricsDto>(req);
    }

    public async Task ClearMessageLogsAsync()
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/messaging/clear");
        await SendAsync(req);
    }

    // --- Module 4: Customers CRUD (Secured with JWT) ---
    public async Task<IReadOnlyList<CustomerDto>> GetCustomersAsync(string? search = null)
    {
        var url = string.IsNullOrWhiteSpace(search) ? "api/customers" : $"api/customers?search={Uri.EscapeDataString(search)}";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<IReadOnlyList<CustomerDto>>(req);
    }

    public async Task<CustomerDto> GetCustomerByIdAsync(Guid id)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"api/customers/{id}");
        return await SendAsync<CustomerDto>(req);
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/customers")
        {
            Content = JsonContent.Create(request)
        };
        return await SendAsync<CustomerDto>(req);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, $"api/customers/{id}")
        {
            Content = JsonContent.Create(request)
        };
        return await SendAsync<CustomerDto>(req);
    }

    public async Task DeleteCustomerAsync(Guid id)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/customers/{id}");
        await SendAsync(req);
    }

    // --- Module 5: Products Catalog & Admin (Secured with JWT & Roles) ---
    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(string? search = null, string? category = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(category)) query.Add($"category={Uri.EscapeDataString(category)}");
        var url = query.Count > 0 ? $"api/products?{string.Join("&", query)}" : "api/products";

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<IReadOnlyList<ProductDto>>(req);
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid id)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"api/products/{id}");
        return await SendAsync<ProductDto>(req);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "api/products")
        {
            Content = JsonContent.Create(request)
        };
        return await SendAsync<ProductDto>(req);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var req = new HttpRequestMessage(HttpMethod.Put, $"api/products/{id}")
        {
            Content = JsonContent.Create(request)
        };
        return await SendAsync<ProductDto>(req);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/products/{id}");
        await SendAsync(req);
    }

    // --- Module 6: Weather API Consumer ---
    public async Task<WeatherDto> GetWeatherAsync(string city)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"api/weather?city={Uri.EscapeDataString(city)}");
        return await SendAsync<WeatherDto>(req);
    }
}
