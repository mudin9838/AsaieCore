using Asaie.BLL.Services;
using Asaie.DAL.Context;
using Asaie.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

// Register PostgreSQL DbContext
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AsaieDbContext>(options =>
    options.UseNpgsql(connString, o => o.UseVector()));

// Register Ollama Client (Local LLM Node running on port 11434)
builder.Services.AddSingleton<IOllamaApiClient>(new OllamaApiClient(new Uri("http://localhost:11434"), "llama3"));

// Register BLL Services
builder.Services.AddScoped<IAgriSovereignService, AgriSovereignService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowReact");
app.UseAuthorization();
app.MapControllers();

app.Run();