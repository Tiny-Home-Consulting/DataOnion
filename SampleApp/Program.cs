using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SampleApp.db;
using TinyHomeDataHelper;
using TinyHomeDataHelper.Config;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var connectionString = "";
services.ConfigureDataHelper<ApplicationContext, NpgsqlConnection>(
    new TinyHomeDataHelperOptions(connectionString)
    {
        EFCore = new EFCoreOptions(
            str => opt => opt.UseNpgsql(str),
            ServiceLifetime.Scoped,
            ServiceLifetime.Scoped
        )
    }
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
