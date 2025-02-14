using AuthAPI.Data;
using AuthAPI.Dtos.Configurations;
using AuthAPI.Filters;
using AuthAPI.Interfaces.Configurations;
using AuthAPI.Interfaces.Services;
using AuthAPI.Models;
using AuthAPI.Repositories;
using AuthAPI.Services;
using AuthAPI.Utilities.Configurations;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("CORSPolicy", builder =>
    {
        builder
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithOrigins("http://localhost:3002");
    });
});

ConfigureConfigurations(builder.Services);

builder.Host.UseSerilog((context, configs) => configs.ReadFrom.Configuration(context.Configuration));

builder.Services.AddDbContext<AppDbContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

ConfigureServices(builder.Services);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ProblemDetailsExceptionFilter>();
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("CORSPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

ApplyMigrations();

app.Run();


void ApplyMigrations()
{
    using (var scope = app.Services.CreateScope())
    {
        var _db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (_db.Database.GetPendingMigrations().Count() > 0)
        {
            _db.Database.Migrate();
        }
    }
}


void ConfigureServices(IServiceCollection services)
{
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IJwtService, JwtService>();
    services.AddScoped<IJwtConfig, JwtConfigs>();

    builder.Services.AddMediatR(typeof(Program));
}

void ConfigureConfigurations(IServiceCollection services)
{
    services
    .AddOptions<JwtOptionsDto>()
    .BindConfiguration("ApiSettings:JwtOptions")
    .ValidateDataAnnotations()
    .ValidateOnStart();
}
