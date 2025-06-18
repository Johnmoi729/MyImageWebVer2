using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using MyImage.Infrastructure.Data;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Core.Interfaces.Services;
using MyImage.Infrastructure.Data.Repositories;
using MyImage.Infrastructure.Services;
using MyImage.API.Middleware;
using MyImage.Core.Entities;
using MongoDB.Bson;

// Configure Serilog for structured logging throughout the application
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/myimage-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting MyImage Photo Printing API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog as the logging provider
    builder.Host.UseSerilog();

    // ============================================================================
    // SERVICE CONFIGURATION
    // ============================================================================

    // Add controllers with API configuration and custom error handling
    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            // Customize model validation error responses to use our ApiResponse format
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                var response = MyImage.Core.DTOs.Common.ApiResponse<object>.ErrorResponse(
                    "Validation failed", errors);

                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
            };
        });

    // Configure CORS for frontend communication
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200", "http://localhost:5159", "https://localhost:7037" };

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Allow cookies/auth headers for JWT tokens
        });
    });

    // ============================================================================
    // DATABASE CONFIGURATION
    // ============================================================================

    // Register MongoDB context as singleton (connection pooling handled by MongoDB driver)
    builder.Services.AddSingleton<MongoDbContext>();

    // Register repositories as scoped services (per-request lifecycle for better transaction consistency)
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IPhotoRepository, PhotoRepository>();
    builder.Services.AddScoped<IPrintSizeRepository, PrintSizeRepository>();
    builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();

    // ============================================================================
    // BUSINESS SERVICES CONFIGURATION
    // ============================================================================

    // Register business services as scoped (per-request lifecycle)
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IGridFsStorageService, GridFsStorageService>();
    builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();

    // ============================================================================
    // AUTHENTICATION AND AUTHORIZATION CONFIGURATION
    // ============================================================================

    // Configure JWT authentication with comprehensive security settings
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var jwtSecret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
    var jwtIssuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
    var jwtAudience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // Allow HTTP in development only
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew for server time differences
            RequireExpirationTime = true
        };

        // Enhanced logging for authentication events to aid in debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Warning("JWT authentication failed for {Path}: {Error}",
                    context.HttpContext.Request.Path, context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var userId = context.Principal?.FindFirst("user_id")?.Value ?? "unknown";
                Log.Debug("JWT token validated for user: {UserId}", userId);
                return Task.CompletedTask;
            }
        };
    });

    // FIXED: Configure authorization policies for role-based access control
    builder.Services.AddAuthorization(options =>
    {
        // REMOVED: Fallback policy that was requiring authentication for ALL endpoints
        // This was causing 401 errors on public endpoints like register/login

        // Admin policy requires admin role for administrative endpoints
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireRole("admin"));

        // Customer policy for customer-specific operations
        options.AddPolicy("CustomerOnly", policy =>
            policy.RequireRole("customer"));

        // Default policy for protected endpoints - only set where explicitly needed
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    // ============================================================================
    // API DOCUMENTATION CONFIGURATION
    // ============================================================================

    // Add API documentation with Swagger/OpenAPI for development
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "MyImage Photo Printing API",
            Version = "v1",
            Description = "API for photo printing service with user management, photo upload, cart, and order processing",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "MyImage Development Team",
                Email = "dev@myimage.com"
            }
        });

        // Configure JWT authentication in Swagger UI for testing
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter 'Bearer' followed by a space and your JWT token"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Include XML documentation if available for better API docs
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // ============================================================================
    // ADDITIONAL SERVICES
    // ============================================================================

    // Add health checks for monitoring and load balancer integration
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database");

    // Configure HTTP client for external services
    builder.Services.AddHttpClient();

    // Add memory caching for performance optimization
    builder.Services.AddMemoryCache();

    // ============================================================================
    // BUILD APPLICATION
    // ============================================================================

    var app = builder.Build();

    // ============================================================================
    // FIXED: MIDDLEWARE PIPELINE CONFIGURATION (ORDER IS CRITICAL)
    // ============================================================================

    // Global error handling (must be first to catch all exceptions)
    app.UseMiddleware<ErrorHandlingMiddleware>();

    // Request logging for monitoring (early in pipeline for complete request tracking)
    app.UseMiddleware<RequestLoggingMiddleware>();

    // Development-specific middleware
    if (app.Environment.IsDevelopment())
    {
        // Enable Swagger UI in development environment only
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyImage API v1");
            options.RoutePrefix = "swagger"; // Access at /swagger
            options.DocumentTitle = "MyImage Photo Printing API";
        });

        // Detailed error pages in development
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // Production security headers
        app.UseHsts(); // HTTP Strict Transport Security
    }

    // Security middleware
    app.UseHttpsRedirection(); // Redirect HTTP to HTTPS

    // CORS (must be before authentication to handle preflight requests)
    app.UseCors("AllowFrontend");

    // Authentication and authorization middleware (order matters)
    app.UseAuthentication(); // JWT token validation
    app.UseAuthorization(); // Role-based access control

    // Controller routing
    app.MapControllers();

    // Health check endpoint for monitoring systems
    app.MapHealthChecks("/health");

    // ============================================================================
    // DATABASE INITIALIZATION AND SEEDING
    // ============================================================================

    // Initialize database and seed essential data
    await InitializeDatabaseAsync(app.Services);

    // ============================================================================
    // START APPLICATION
    // ============================================================================

    Log.Information("MyImage API starting on {Environment} environment", app.Environment.EnvironmentName);
    Log.Information("API documentation available at: /swagger (development only)");
    Log.Information("Health check available at: /health");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Initialize database connection and seed initial data.
/// This method ensures the database is ready and contains essential startup data.
/// </summary>
/// <param name="services">Service provider for dependency injection</param>
/// <returns>Task representing the asynchronous initialization</returns>
static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    try
    {
        Log.Information("Initializing database...");

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MongoDbContext>();

        // Test database connectivity before proceeding
        var isConnected = await context.TestConnectionAsync();
        if (!isConnected)
        {
            throw new InvalidOperationException("Unable to connect to MongoDB database");
        }

        Log.Information("Database connection established successfully");

        // Seed initial data required for application operation
        await SeedInitialDataAsync(scope.ServiceProvider);

        Log.Information("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to initialize database");
        throw;
    }
}

/// <summary>
/// Seed initial data required for application operation.
/// Creates default print sizes, admin user, and system settings.
/// </summary>
/// <param name="serviceProvider">Service provider for accessing repositories</param>
/// <returns>Task representing the asynchronous seeding</returns>
static async Task SeedInitialDataAsync(IServiceProvider serviceProvider)
{
    try
    {
        Log.Information("Seeding initial data...");

        var printSizeRepo = serviceProvider.GetRequiredService<IPrintSizeRepository>();
        var userRepo = serviceProvider.GetRequiredService<IUserRepository>();
        var settingsRepo = serviceProvider.GetRequiredService<ISystemSettingsRepository>();
        var authService = serviceProvider.GetRequiredService<IAuthService>();

        // Seed print sizes if none exist
        var existingPrintSizes = await printSizeRepo.GetAllAsync();
        if (existingPrintSizes.Count == 0)
        {
            Log.Information("Seeding default print sizes...");

            var defaultPrintSizes = new[]
            {
                new PrintSize
                {
                    SizeCode = "4x6",
                    DisplayName = "Standard 4×6",
                    Dimensions = new PrintDimensions
                    {
                        Width = 4, Height = 6, Unit = "inches",
                        PixelRequirements = new PixelRequirements
                        {
                            MinWidth = 1200, MinHeight = 1800,
                            RecommendedWidth = 2400, RecommendedHeight = 3600
                        }
                    },
                    Pricing = new PrintPricing { BasePrice = 0.29m, Currency = "USD", UpdatedBy = "system" },
                    Metadata = new PrintSizeMetadata { IsActive = true, SortOrder = 1 }
                },
                new PrintSize
                {
                    SizeCode = "5x7",
                    DisplayName = "Classic 5×7",
                    Dimensions = new PrintDimensions
                    {
                        Width = 5, Height = 7, Unit = "inches",
                        PixelRequirements = new PixelRequirements
                        {
                            MinWidth = 1500, MinHeight = 2100,
                            RecommendedWidth = 3000, RecommendedHeight = 4200
                        }
                    },
                    Pricing = new PrintPricing { BasePrice = 0.49m, Currency = "USD", UpdatedBy = "system" },
                    Metadata = new PrintSizeMetadata { IsActive = true, SortOrder = 2 }
                },
                new PrintSize
                {
                    SizeCode = "8x10",
                    DisplayName = "Large 8×10",
                    Dimensions = new PrintDimensions
                    {
                        Width = 8, Height = 10, Unit = "inches",
                        PixelRequirements = new PixelRequirements
                        {
                            MinWidth = 2400, MinHeight = 3000,
                            RecommendedWidth = 4800, RecommendedHeight = 6000
                        }
                    },
                    Pricing = new PrintPricing { BasePrice = 2.99m, Currency = "USD", UpdatedBy = "system" },
                    Metadata = new PrintSizeMetadata { IsActive = true, SortOrder = 3 }
                }
            };

            foreach (var printSize in defaultPrintSizes)
            {
                await printSizeRepo.CreateAsync(printSize);
            }

            Log.Information("Seeded {Count} default print sizes", defaultPrintSizes.Length);
        }

        // Create default admin user if none exists
        var adminEmail = "admin@myimage.com";
        var existingAdmin = await userRepo.GetByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            Log.Information("Creating default admin user...");

            var adminUser = await authService.RegisterAsync(
                adminEmail,
                "Admin123!@#", // Strong default password - should be changed after first login
                "System",
                "Administrator");

            // Update role to admin through repository
            adminUser.Role = "admin";
            await userRepo.UpdateAsync(adminUser);

            Log.Information("Created default admin user: {UserId} ({Email})", adminUser.UserId, adminEmail);
            Log.Warning("Default admin password is 'Admin123!@#' - CHANGE IMMEDIATELY IN PRODUCTION");
        }

        // Seed system settings with proper _id handling
        await SeedSystemSettingsAsync(settingsRepo);

        Log.Information("Initial data seeding completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to seed initial data");
        throw;
    }
}

/// <summary>
/// Seed system settings with default configuration values.
/// Creates tax rates, branch locations, and other system configuration.
/// Now properly handles MongoDB _id field immutability by checking for existence first.
/// </summary>
/// <param name="settingsRepo">System settings repository</param>
/// <returns>Task representing the asynchronous seeding</returns>
static async Task SeedSystemSettingsAsync(ISystemSettingsRepository settingsRepo)
{
    Log.Information("Seeding system settings...");

    try
    {
        // Check if tax rates setting already exists before creating
        var existingTaxRates = await settingsRepo.GetByKeyAsync("tax_rates");
        if (existingTaxRates == null)
        {
            // Tax rates by state - Do NOT set Id explicitly, let MongoDB generate it
            var taxRatesSetting = new SystemSettings
            {
                Key = "tax_rates",
                Value = new BsonDocument
                {
                    ["default"] = 0.0625, // 6.25% default rate
                    ["byState"] = new BsonDocument
                    {
                        ["MA"] = 0.0625, // Massachusetts
                        ["NH"] = 0.0000, // New Hampshire (no sales tax)
                        ["CT"] = 0.0635, // Connecticut
                        ["RI"] = 0.0700, // Rhode Island
                        ["VT"] = 0.0600, // Vermont
                        ["NY"] = 0.0800  // New York
                    }
                },
                Metadata = new SettingMetadata
                {
                    Description = "Sales tax rates by state for order calculations",
                    UpdatedBy = "system",
                    LastUpdated = DateTime.UtcNow
                }
            };

            await settingsRepo.UpsertAsync(taxRatesSetting);
            Log.Information("Seeded tax rates setting");
        }

        // Check if branch locations setting already exists before creating
        var existingBranchLocations = await settingsRepo.GetByKeyAsync("branch_locations");
        if (existingBranchLocations == null)
        {
            // Branch locations for in-person payment - Do NOT set Id explicitly
            var branchLocationsSetting = new SystemSettings
            {
                Key = "branch_locations",
                Value = new BsonDocument
                {
                    ["locations"] = new BsonArray
                    {
                        new BsonDocument
                        {
                            ["name"] = "Boston Downtown",
                            ["address"] = "123 Main Street, Boston, MA 02101",
                            ["phone"] = "+1-617-555-0100",
                            ["hours"] = "Mon-Fri 9AM-6PM, Sat 10AM-4PM"
                        },
                        new BsonDocument
                        {
                            ["name"] = "Cambridge Center",
                            ["address"] = "456 Tech Boulevard, Cambridge, MA 02139",
                            ["phone"] = "+1-617-555-0200",
                            ["hours"] = "Mon-Fri 9AM-6PM, Sat 10AM-4PM"
                        }
                    }
                },
                Metadata = new SettingMetadata
                {
                    Description = "Branch locations for in-person payment option",
                    UpdatedBy = "system",
                    LastUpdated = DateTime.UtcNow
                }
            };

            await settingsRepo.UpsertAsync(branchLocationsSetting);
            Log.Information("Seeded branch locations setting");
        }

        // Check if photo cleanup settings already exist before creating
        var existingCleanupSettings = await settingsRepo.GetByKeyAsync("photo_cleanup_settings");
        if (existingCleanupSettings == null)
        {
            // Photo cleanup settings - Do NOT set Id explicitly
            var cleanupSettings = new SystemSettings
            {
                Key = "photo_cleanup_settings",
                Value = new BsonDocument
                {
                    ["retentionDays"] = 7, // Keep photos 7 days after order completion
                    ["cleanupSchedule"] = "daily", // Run cleanup daily
                    ["bufferDays"] = 3 // Additional buffer for customer service
                },
                Metadata = new SettingMetadata
                {
                    Description = "Photo cleanup schedule and retention policies",
                    UpdatedBy = "system",
                    LastUpdated = DateTime.UtcNow
                }
            };

            await settingsRepo.UpsertAsync(cleanupSettings);
            Log.Information("Seeded photo cleanup settings");
        }

        Log.Information("System settings seeding completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to seed system settings: {Error}", ex.Message);
        throw;
    }
}

/// <summary>
/// Health check implementation for database connectivity monitoring.
/// Used by load balancers and monitoring systems to verify service health.
/// </summary>
public class DatabaseHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly MongoDbContext _context;

    public DatabaseHealthCheck(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test database connectivity with proper timeout handling
            var isConnected = await _context.TestConnectionAsync();

            if (isConnected)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database connection is healthy");
            }
            else
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Unable to connect to database");
            }
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}