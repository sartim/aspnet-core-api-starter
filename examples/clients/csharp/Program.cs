using System.Net.Http.Headers;
using System.Net.Http.Json;

var apiUrl = Environment.GetEnvironmentVariable("API_URL") ?? "http://localhost:5070";
using var http = new HttpClient { BaseAddress = new Uri(apiUrl) };

// Generate a typed client from /swagger/v1/swagger.json for production use.
// This small example shows the same authentication and pagination flow without
// adding an SDK dependency to the starter repository.
var email = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@example.com";
var password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "change-this-password";
var login = await http.PostAsJsonAsync("/api/v1/auth/generate-jwt", new { email, password });
login.EnsureSuccessStatusCode();
var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())?.Token
    ?? throw new InvalidOperationException("The login response did not include a token.");

http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
var users = await http.GetFromJsonAsync<PagedResponse<User>>(
    "/api/v1/users?page=1&pageSize=25")
    ?? throw new InvalidOperationException("The users response was empty.");

Console.WriteLine($"Loaded {users.Items.Count} of {users.TotalCount} users.");

public sealed record LoginResponse(string Token);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);
public sealed record User(Guid Id, string FirstName, string LastName, string Email, int Phone);
