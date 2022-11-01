using DataOnion;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SampleApp.db;
using SampleApp.Models;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var dbConnectionString = "";
var redisConnectionString = "";
var environment = "DEV";
var authPrefix = "usersession";

services.AddDatabaseOnion(dbConnectionString)
    .ConfigureDapper<NpgsqlConnection>(str => new NpgsqlConnection(str))
    .ConfigureEfCore<ApplicationContext>(str => opt => opt.UseNpgsql(str));

services.AddAuthOnion(environment)
    .ConfigureSlidingExpiration<LoginData>(
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(12),
        authPrefix,
        hash => new LoginData(hash)
    )
    .ConfigureRedis(redisConnectionString)
    .ConfigureTwoFactorAuth();


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
