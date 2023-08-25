using Microsoft.EntityFrameworkCore;
using newstrek.Data;
using newstrek.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

DotNetEnv.Env.Load();
var DbConnectionString = System.Environment.GetEnvironmentVariable("NewsTrekDbConnectionString");
builder.Services.AddDbContext<NewsTrekDbContext>(options => options.UseSqlServer(DbConnectionString));

builder.Services.AddElasticSearch(builder.Configuration);

builder.Services.AddHttpClient();

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

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
