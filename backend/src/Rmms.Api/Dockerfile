# ============== Rmms.Api Dockerfile ==============
# Multi-stage build: SDK → runtime, non-root user, healthcheck.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Non-root user
RUN groupadd --system --gid 1001 rmms \
 && useradd  --system --uid 1001 --gid rmms --no-create-home rmms

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
COPY ["src/Rmms.Api/Rmms.Api.csproj",                       "src/Rmms.Api/"]
COPY ["src/Rmms.Application/Rmms.Application.csproj",       "src/Rmms.Application/"]
COPY ["src/Rmms.Domain/Rmms.Domain.csproj",                 "src/Rmms.Domain/"]
COPY ["src/Rmms.Infrastructure/Rmms.Infrastructure.csproj", "src/Rmms.Infrastructure/"]
COPY ["src/Rmms.Shared/Rmms.Shared.csproj",                 "src/Rmms.Shared/"]
RUN dotnet restore "src/Rmms.Api/Rmms.Api.csproj"

COPY . .
WORKDIR /src/src/Rmms.Api
RUN dotnet publish "Rmms.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN chown -R rmms:rmms /app
USER rmms
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD wget -qO- http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Rmms.Api.dll"]
