using Microsoft.EntityFrameworkCore;
using newstrek.Data;
using newstrek.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using newstrek.Configurations;
using newstrek.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
DotNetEnv.Env.Load();
var OpenAiKey = System.Environment.GetEnvironmentVariable("OpenAI_ApiKey");
builder.Services.Configure<OpenAiConfig>(options => options.Key = OpenAiKey);

builder.Services.AddControllers();

var DbConnectionString = System.Environment.GetEnvironmentVariable("NewsTrekDbConnectionString");
builder.Services.AddDbContext<NewsTrekDbContext>(options => options.UseSqlServer(DbConnectionString));

builder.Services.AddScoped<IOpenAiService, OpenAiService>();

builder.Services.AddElasticSearch(builder.Configuration);

builder.Services.AddHttpClient();

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateAudience = false,
        ValidateIssuer = false,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            builder.Configuration.GetSection("Jwt:SecretKey").Value!))
    };
});

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
