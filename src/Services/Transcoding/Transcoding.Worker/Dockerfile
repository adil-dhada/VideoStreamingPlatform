FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Services/Transcoding/Transcoding.Worker/Transcoding.Worker.csproj"
RUN dotnet build "src/Services/Transcoding/Transcoding.Worker/Transcoding.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/Transcoding/Transcoding.Worker/Transcoding.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Transcoding.Worker.dll"]
