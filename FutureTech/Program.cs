using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using Microsoft.Azure.Cosmos;
using FutureTech.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure file upload size limit to 10MB
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

// Configure authentication with Google
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Google";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.SlidingExpiration = true;
})
.AddGoogle("Google", options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    
    // Add additional claims to retrieve from Google
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.SaveTokens = true;
    
    options.Events.OnCreatingTicket = context =>
    {
        // Map claims from Google to our application
        var picture = context.User.GetProperty("picture").GetString();
        if (!string.IsNullOrEmpty(picture))
        {
            context.Identity.AddClaim(new System.Security.Claims.Claim("urn:google:picture", picture));
        }
        
        return Task.CompletedTask;
    };
});

// Add Authorization policy for Admin users
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            if (context.User.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                var email = context.User.FindFirstValue(ClaimTypes.Email);
                var adminEmails = builder.Configuration.GetSection("Authentication:AdminEmails").Get<string[]>();
                return adminEmails?.Contains(email) == true;
            }
            return false;
        });
    });
});

// Register Cosmos DB service
string cosmosDbConnectionString = builder.Configuration["ConnectionStrings:CosmosDbConnection"];
string databaseName = "FutureTechAcademy";
string containerName = "Students";

try
{
    builder.Services.AddSingleton<ICosmosDbService>(InitializeCosmosClientInstanceAsync(cosmosDbConnectionString, databaseName, containerName).GetAwaiter().GetResult());
    Console.WriteLine("Successfully initialized Cosmos DB client.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error initializing Cosmos DB client: {ex.Message}");
    throw; // Rethrow to prevent app from starting with invalid Cosmos DB configuration
}

// Register Blob Storage service
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Method to initialize Cosmos DB client
static async Task<CosmosDbService> InitializeCosmosClientInstanceAsync(string connectionString, string databaseName, string containerName)
{
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new ArgumentException("Cosmos DB connection string is missing in configuration.", nameof(connectionString));
    }

    try
    {
        Console.WriteLine($"Initializing Cosmos DB client with database '{databaseName}' and container '{containerName}'");
        
        var cosmosClientOptions = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            },
            ConnectionMode = ConnectionMode.Gateway // More firewall-friendly mode
        };

        CosmosClient client = new CosmosClient(connectionString, cosmosClientOptions);
        
        Console.WriteLine("Creating database if not exists...");
        DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
        
        Console.WriteLine("Creating container if not exists...");
        await database.Database.CreateContainerIfNotExistsAsync(
            id: containerName, 
            partitionKeyPath: "/id",
            throughput: 400 // Minimum throughput
        );
        
        Console.WriteLine("Cosmos DB setup completed successfully.");
        return new CosmosDbService(client, databaseName, containerName);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to initialize Cosmos DB: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        throw;
    }
}
