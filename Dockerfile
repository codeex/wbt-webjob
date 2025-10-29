# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY WbtWebJob.sln .
COPY src/WbtWebJob.Api/WbtWebJob.Api.csproj src/WbtWebJob.Api/
COPY src/WbtWebJob.Core/WbtWebJob.Core.csproj src/WbtWebJob.Core/
COPY src/WbtWebJob.Infrastructure/WbtWebJob.Infrastructure.csproj src/WbtWebJob.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY . .

# Build and publish
WORKDIR /src/src/WbtWebJob.Api
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "WbtWebJob.Api.dll"]
