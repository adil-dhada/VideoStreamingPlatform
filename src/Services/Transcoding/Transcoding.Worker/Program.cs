using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.BuildingBlocks;
using Transcoding.Application.Abstractions;
using Transcoding.Application.Consumers;
using Transcoding.Domain;
using Transcoding.Infrastructure.Data;
using Transcoding.Infrastructure.Services;
using System;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    var configuration = hostContext.Configuration;

    services.AddDbContext<TranscodingDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("Default")));

    services.AddScoped<ITranscodingJobRepository, TranscodingJobRepository>();
    services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TranscodingDbContext>());
    services.AddScoped<IFFmpegTranscodingService, FFmpegTranscodingService>();

    var blobRoot = configuration["BlobStorage:Root"] ?? "/blob";
    services.AddSingleton<IBlobStorageService>(new LocalBlobStorageService(blobRoot));

    services.AddMassTransit(x =>
    {
        x.AddConsumer<VideoUploadedConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(configuration["RabbitMq:Host"] ?? "rabbitmq", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            cfg.ReceiveEndpoint("video-uploaded", e =>
            {
                e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(5)));
                e.ConfigureConsumer<VideoUploadedConsumer>(ctx);
            });
        });
    });
});

var host = builder.Build();
host.Run();
