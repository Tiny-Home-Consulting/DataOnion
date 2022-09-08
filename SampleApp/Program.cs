using Microsoft.EntityFrameworkCore;
using Npgsql;
using SampleApp.db;
using TinyHomeDataHelper;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var connectionString = "";

services.ConfigureDapperDataHelper<NpgsqlConnection>(
    new(
        connectionString,
        str => new NpgsqlConnection(str)
    )
);

services.ConfigureEfCoreDataHelper<ApplicationContext>(
    new(
        connectionString,
        str => opt => opt.UseNpgsql(str),
        ServiceLifetime.Scoped,
        ServiceLifetime.Scoped
    )
);


    Func<string, Action<DbContextOptionsBuilder>> test = str => opt => opt.UseNpgsql(str);















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
