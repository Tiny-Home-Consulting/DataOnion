using DataOnion;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SampleApp.db;
using StackExchange.Redis;
using SampleApp.Models;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var dbConnectionString = "";
var redisConnectionString = "";

services.AddDatabaseOnion(dbConnectionString)
    .ConfigureDapper<NpgsqlConnection>(str => new NpgsqlConnection(str))
    .ConfigureEfCore<ApplicationContext>(str => opt => opt.UseNpgsql(str));

services.AddAuthOnion()
    .ConfigureRedis(redisConnectionString)
    .ConfigureSlidingExpiration<LoginData>(
        TimeSpan.FromMinutes(30),
        x => x.SessionId.ToString(),
        (x, y) => x.SessionId == y.SessionId,
        x => new[]
        {
            new HashEntry("username", x.Username),
            new HashEntry("password", x.Password),
            new HashEntry("session-id", x.SessionId.ToString())
        },
        hash => {
            var dict = hash.ToStringDictionary();
            return new LoginData
            {
                Username = dict["username"],
                Password = dict["password"],
                SessionId = Guid.Parse(dict["session-id"])
            };
        }
    );


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
