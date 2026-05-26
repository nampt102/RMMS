# ============== Rmms.Worker Dockerfile ==============
# Hangfire.AspNetCore needs ASP.NET Core shared framework → use aspnet image, not runtime.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV DOTNET_RUNNING_IN_CONTAINER=true

RUN groupadd --system --gid 1001 rmms \
 && useradd  --system --uid 1001 --gid rmms --no-create-home rmms

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
COPY ["src/Rmms.Worker/Rmms.Worker.csproj",                "src/Rmms.Worker/"]
COPY ["src/Rmms.Application/Rmms.Application.csproj",      "src/Rmms.Application/"]
COPY ["src/Rmms.Domain/Rmms.Domain.csproj",                "src/Rmms.Domain/"]
COPY ["src/Rmms.Infrastructure/Rmms.Infrastructure.csproj","src/Rmms.Infrastructure/"]
COPY ["src/Rmms.Shared/Rmms.Shared.csproj",                "src/Rmms.Shared/"]
RUN dotnet restore "src/Rmms.Worker/Rmms.Worker.csproj"

COPY . .
WORKDIR /src/src/Rmms.Worker
RUN dotnet publish "Rmms.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN chown -R rmms:rmms /app
USER rmms
ENTRYPOINT ["dotnet", "Rmms.Worker.dll"]
