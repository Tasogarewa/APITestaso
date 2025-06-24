using AspNetCoreRateLimit;
using Backend.AppDbContext;
using Backend.DTOs.Validators;
using Backend.Middlewares;
using Backend.Models;
using Backend.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Serilog;
using System.Text;
internal class Program
{
    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
  .MinimumLevel.Debug()
  .WriteTo.Console()
  .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
  .CreateLogger();

        var builder = WebApplication.CreateBuilder(args);

        var jwtKey = builder.Configuration["Jwt:Key"];
        var jwtIssuer = builder.Configuration["Jwt:Issuer"];
        var jwtAudience = builder.Configuration["Jwt:Audience"];
        builder.Services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();

           
            q.UsePersistentStore(options =>
            {
                options.UseProperties = true;
                options.UseSqlServer(sqlServerOptions =>
                {
                    sqlServerOptions.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                });

                options.UseJsonSerializer(); 
            });
        });
        
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddFluentValidationClientsideAdapters();
        builder.Services.AddValidatorsFromAssemblyContaining<ApiTestDtoValidator>();
        builder.Services.AddValidatorsFromAssemblyContaining<ApiTestScenarioCreateDtoValidator>();
        builder.Services.AddValidatorsFromAssemblyContaining<ChangePasswordDtoValidator>();
        builder.Services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();
        builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
        builder.Services.AddValidatorsFromAssemblyContaining<ScenarioScheduleDtoValidator>();
        builder.Services.AddValidatorsFromAssemblyContaining<SqlTestDtoValidator>();
        builder.Services.AddValidatorsFromAssemblyContaining<UpdateProfileDtoValidator>();
        builder.Host.UseSerilog();
        builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        builder.Services.AddScoped<JwtService>();
        builder.Services.AddScoped<TestComparisonService>();
        builder.Services.AddScoped<SchedulerService>();
        builder.Services.AddScoped<TestRunnerService>();
        builder.Services.AddScoped<TestStatisticsService>();
        builder.Services.AddHttpClient();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", builder =>
            {
                builder.WithOrigins("https://localhost:7292") 
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials(); 
            });
        });

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Enter 'Bearer' [space] and then your token."
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
            new string[] {}
        }
    });
        });
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
      options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
      
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
            };
        });
        builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"./keys/"))
    .SetApplicationName("APITestasoApp");
        builder.Logging.AddConsole();
        builder.Services.AddAuthorization();
        builder.Services.AddMemoryCache();
        builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
        builder.Services.AddInMemoryRateLimiting();
        builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        var app = builder.Build();
        
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseIpRateLimiting();
        app.Use(async (context, next) =>
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault();
            Console.WriteLine($"Token: {token}");
            await next();
        });
        app.UseHttpsRedirection();
        app.UseRouting();
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            await next();
        });
        app.UseCors("AllowFrontend");
        app.UseHttpsRedirection();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseAuthentication(); 
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}