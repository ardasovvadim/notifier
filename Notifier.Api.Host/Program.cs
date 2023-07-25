using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Notifier.Api.Host.Contracts;
using Notifier.Api.Host.Services;
using Notifier.Database.Database;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var services = builder.Services;
var configuration = builder.Configuration;

services.AddDbContext<NContext>(options => { options.UseMySQL(configuration.GetConnectionString("DefaultConnection")!); });

services.AddScoped<ITelegramService, TelegramService>();
services.AddScoped<ITelegramBotClient, TelegramBotClient>(sp =>
{
    var secret = configuration.GetSection(TelegramSettings.Name)["Secret"];
    return new TelegramBotClient(secret!);
});
services.Configure<TelegramSettings>(configuration.GetSection(TelegramSettings.Name));

services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();