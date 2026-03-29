using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.BuildingBlocks;
using Streaming.API.Services;

var builder = WebApplication.CreateBuilder(args);

var blobRoot = builder.Configuration["BlobStorage:Root"] ?? "/blob";
builder.Services.AddSingleton<IBlobStorageService>(new LocalBlobStorageService(blobRoot));

builder.Services.AddHttpClient<IVideoMetadataClient, VideoMetadataClient>(client =>
{
    var baseAddr = builder.Configuration["VideoManagementApiBaseUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new System.Uri(baseAddr);
});

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", x =>
    {
        x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

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

app.UseCors("AllowAll");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
