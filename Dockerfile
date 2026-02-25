# Ouroboros CLI Dockerfile
# Multi-stage build for optimized production image

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and Directory.Build.props
COPY Ouroboros.slnx .
COPY Directory.Build.props .

# Copy submodule project files for restore
COPY .build/ .build/
COPY foundation/src/Ouroboros.Core/Ouroboros.Core.csproj foundation/src/Ouroboros.Core/
COPY foundation/src/Ouroboros.Domain/Ouroboros.Domain.csproj foundation/src/Ouroboros.Domain/
COPY foundation/src/Ouroboros.Genetic/Ouroboros.Genetic.csproj foundation/src/Ouroboros.Genetic/
COPY foundation/src/Ouroboros.Roslynator/Ouroboros.Roslynator.csproj foundation/src/Ouroboros.Roslynator/
COPY foundation/src/Ouroboros.Tools/Ouroboros.Tools.csproj foundation/src/Ouroboros.Tools/
COPY engine/src/Ouroboros.Agent/Ouroboros.Agent.csproj engine/src/Ouroboros.Agent/
COPY engine/src/Ouroboros.Network/Ouroboros.Network.csproj engine/src/Ouroboros.Network/
COPY engine/src/Ouroboros.Pipeline/Ouroboros.Pipeline.csproj engine/src/Ouroboros.Pipeline/
COPY engine/src/Ouroboros.Providers/Ouroboros.Providers.csproj engine/src/Ouroboros.Providers/
COPY app/src/Ouroboros.Application/Ouroboros.Application.csproj app/src/Ouroboros.Application/
COPY app/src/Ouroboros.CLI/Ouroboros.CLI.csproj app/src/Ouroboros.CLI/
COPY app/src/Ouroboros.Easy/Ouroboros.Easy.csproj app/src/Ouroboros.Easy/
COPY app/src/Ouroboros.Examples/Ouroboros.Examples.csproj app/src/Ouroboros.Examples/
COPY app/src/Ouroboros.WebApi/Ouroboros.WebApi.csproj app/src/Ouroboros.WebApi/

# Restore dependencies
RUN dotnet restore app/src/Ouroboros.CLI/Ouroboros.CLI.csproj

# Copy source code
COPY . .

# Build and publish
WORKDIR /src/app/src/Ouroboros.CLI
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Install dependencies for MeTTa support (optional)
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Copy configuration files
COPY appsettings.json .
COPY appsettings.Production.json .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=Production

# Expose health check port (if needed for future enhancements)
EXPOSE 8080

# Set entrypoint
ENTRYPOINT ["dotnet", "Ouroboros.CLI.dll"]
