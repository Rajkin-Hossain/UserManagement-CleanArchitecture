using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using FluentValidation;
using MongoDB.Bson.Serialization.Conventions;
using UserManagement.Core.Interfaces;
using UserManagement.Infrastructure.Persistence;
using UserManagement.Infrastructure.Security;
using UserManagement.Infrastructure.Services;
using UserManagement.Application.Interfaces;
using UserManagement.Application.Services;
using UserManagement.Application.Validators;
using UserManagement.Application.DTOs;

var builder = WebApplication.CreateBuilder(args);

// MongoDB Conventions
var pack = new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreExtraElementsConvention(true) };
ConventionRegistry.Register("CustomConventions", pack, t => true);

// Configure MongoDB
var mongoSettings = builder.Configuration.GetSection("MongoDb");
builder.Services.AddSingleton(new MongoDbContext(
    mongoSettings["ConnectionString"] ?? "mongodb://localhost:27017",
    mongoSettings["DatabaseName"] ?? "UserManagement"
));

// Registrations
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IRiskService, RiskService>();
builder.Services.AddScoped<IUserService, UserService>();

// FluentValidation
builder.Services.AddScoped<IValidator<RegisterUserDto>, RegisterUserDtoValidator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
