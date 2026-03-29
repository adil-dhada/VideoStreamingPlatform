using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.BuildingBlocks;
using VideoManagement.Application.Abstractions;
using VideoManagement.Domain;
using VideoManagement.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<VideoManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IUploadSessionRepository, UploadSessionRepository>();
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<VideoManagementDbContext>());

var blobRoot = builder.Configuration["BlobStorage:Root"] ?? "/blob";
builder.Services.AddSingleton<IBlobStorageService>(new LocalBlobStorageService(blobRoot));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(VideoManagement.Application.Commands.InitiateUploadCommand).Assembly));

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddControllers();

var secret = builder.Configuration["Jwt:Secret"];
if (!string.IsNullOrEmpty(secret))
{
    var key = Encoding.UTF8.GetBytes(secret);
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true
            };
        });
}

var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
