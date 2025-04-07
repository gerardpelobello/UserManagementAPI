var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Exception handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unhandled Exception: {ex.Message}");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "An unexpected error occurred. Please try again later."
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }
});

// Token validation middleware
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var token))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new
        {
            StatusCode = context.Response.StatusCode,
            Message = "Authorization token is missing."
        });
        return;
    }

    if (!IsValidToken(token))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new
        {
            StatusCode = context.Response.StatusCode,
            Message = "Invalid or expired token."
        });
        return;
    }

    await next();
});

// Logging middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next();
    Console.WriteLine($"Response: {context.Response.StatusCode}");
});


// In-memory user dictionary for demonstration purposes
var users = new Dictionary<int, User>
{
    { 1, new User { Id = 1, Name = "John Doe", Email = "john.doe@example.com" } },
    { 2, new User { Id = 2, Name = "Jane Smith", Email = "jane.smith@example.com" } }
};

// GET: Retrieve all users
app.MapGet("/users", () => Results.Ok(users.Values));

// GET: Retrieve a specific user by ID
app.MapGet("/users/{id:int}", (int id) =>
{
    if (users.TryGetValue(id, out var user))
        return Results.Ok(user);
    return Results.NotFound();
});

// POST: Add a new user
app.MapPost("/users", (User newUser) =>
{
    if (string.IsNullOrWhiteSpace(newUser.Name))
        return Results.BadRequest("Name cannot be empty.");
    if (!IsValidEmail(newUser.Email))
        return Results.BadRequest("Invalid email format.");

    newUser.Id = users.Any() ? users.Keys.Max() + 1 : 1;
    users[newUser.Id] = newUser;
    return Results.Created($"/users/{newUser.Id}", newUser);
});

// PUT: Update an existing user's details
app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    if (string.IsNullOrWhiteSpace(updatedUser.Name))
        return Results.BadRequest("Name cannot be empty.");
    if (!IsValidEmail(updatedUser.Email))
        return Results.BadRequest("Invalid email format.");

    if (!users.TryGetValue(id, out var user))
        return Results.NotFound();

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    return Results.NoContent();
});

// DELETE: Remove a user by ID
app.MapDelete("/users/{id:int}", (int id) =>
{
    if (!users.Remove(id))
        return Results.NotFound();

    return Results.NoContent();
});

app.Run();

bool IsValidToken(string token)
{
    return token == "valid-token"; // Replace with actual validation logic
}

// Helper method to validate email format
bool IsValidEmail(string email)
{
    return !string.IsNullOrWhiteSpace(email) &&
           System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}

record User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

