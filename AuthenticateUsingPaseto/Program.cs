using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using WebApiPaseto;
using WebApiPaseto.Model;
using WebApiPaseto.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<PasetoService>();
builder.Services.AddAuthentication("PasetoScheme")
    .AddScheme<AuthenticationSchemeOptions, PasetoAuthenticationHandler>("PasetoScheme", null);
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Login endpoint
app.MapPost("/login", (LoginRequest request, PasetoService pasetoService) =>
{
    // Validate credentials (placeholder logic)
    if (request.Username == "demo" && request.Password == "password")
    {
        var userId = "user123";
        var email = "demo@example.com";

        var token = pasetoService.GenerateToken(userId, email);

        return Results.Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Username = request.Username
        });
    }

    return Results.Unauthorized();
});

// Get profile endpoint
app.MapGet("/profile", (ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var email = user.FindFirst("email")?.Value;

    return Results.Ok(new UserProfile
    {
        Id = 1,
        Username = userId ?? "unknown",
        Email = email ?? "unknown@example.com",
        FullName = "Demo User",
        CreatedAt = DateTime.UtcNow.AddMonths(-6)
    });
}).RequireAuthorization();

// Get order information endpoint
app.MapGet("/orders", (ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    // In a real app, you'd get orders for the authenticated user
    var orders = new List<Order>
    {
        new Order { Id = 1, ProductName = "Product A", Quantity = 2, TotalAmount = 99.99m, OrderDate = DateTime.UtcNow.AddDays(-5) },
        new Order { Id = 2, ProductName = "Product B", Quantity = 1, TotalAmount = 49.99m, OrderDate = DateTime.UtcNow.AddDays(-2) }
    };

    return Results.Ok(orders);
}).RequireAuthorization();

app.Run();

// Response models

record LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Username { get; set; } = string.Empty;
}

record UserProfile
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

record Order
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}
