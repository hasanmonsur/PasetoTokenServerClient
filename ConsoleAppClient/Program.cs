using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;

// Simple client for the Paseto sample API

var baseUrl = args.Length > 0
    ? args[0]
    : GetInput("API base URL (default https://localhost:7285): ", "https://localhost:7285");

var username = GetInput("Username (default demo): ", "demo");
var password = GetInput("Password (default password): ", "password");

// Ensure base URL ends with /
if (!baseUrl.EndsWith("/"))
    baseUrl += "/";

// Disable auto redirect (useful for debugging auth issues)
using var handler = new HttpClientHandler { AllowAutoRedirect = false };
using var client = new HttpClient(handler)
{
    BaseAddress = new Uri(baseUrl)
};

try
{
    // 🔐 LOGIN
    var loginRequest = new LoginRequest
    {
        Username = username,
        Password = password
    };

    var loginResp = await client.PostAsJsonAsync("login", loginRequest);

    if (!loginResp.IsSuccessStatusCode)
    {
        Console.WriteLine($"❌ Login failed: {loginResp.StatusCode}");
        Console.WriteLine(await loginResp.Content.ReadAsStringAsync());
        return;
    }

    var login = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

    if (login == null || string.IsNullOrEmpty(login.Token))
    {
        Console.WriteLine("❌ Invalid login response (no token)");
        return;
    }

    Console.WriteLine($"✅ Token received (expires: {login.ExpiresAt})");
    Console.WriteLine(login.Token);
    Console.WriteLine();

    // Attach token
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", login.Token);

    // 👤 PROFILE
    var profileResp = await client.GetAsync("profile");

    if (profileResp.IsSuccessStatusCode)
    {
        var profile = await profileResp.Content.ReadFromJsonAsync<UserProfile>();

        Console.WriteLine("👤 Profile:");
        Console.WriteLine(JsonSerializer.Serialize(profile, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
    else
    {
        Console.WriteLine($"❌ Profile failed: {profileResp.StatusCode}");
        Console.WriteLine(await profileResp.Content.ReadAsStringAsync());
    }

    // 📦 ORDERS
    var ordersResp = await client.GetAsync("orders");

    if (ordersResp.IsSuccessStatusCode)
    {
        var orders = await ordersResp.Content.ReadFromJsonAsync<Order[]>();

        Console.WriteLine("📦 Orders:");
        Console.WriteLine(JsonSerializer.Serialize(orders, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
    else
    {
        Console.WriteLine($"❌ Orders failed: {ordersResp.StatusCode}");
        Console.WriteLine(await ordersResp.Content.ReadAsStringAsync());
    }

    GetInput("Press ENTER to quit...", "");
}
catch (HttpRequestException httpEx)
{
    Console.WriteLine($"🌐 HTTP Error: {httpEx.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"🔥 Error: {ex.Message}");
}


// 🔧 Helper
string GetInput(string prompt, string defaultValue)
{
    Console.Write(prompt);
    var input = Console.ReadLine();
    return string.IsNullOrWhiteSpace(input) ? defaultValue : input.Trim();
}


// 📦 Models

public class LoginRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class UserProfile
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}