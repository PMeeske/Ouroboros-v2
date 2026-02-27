FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy build infrastructure first (rarely changes — cached)
COPY .build/ .build/
COPY Directory.Build.props .

# Copy foundation (needed by Hypergrid.Host → Abstractions)
COPY foundation/src/ foundation/src/

# Copy engine (needed by Hypergrid.Mesh → Agent)
COPY engine/src/ engine/src/

# Copy all hypergrid projects
COPY hypergrid/src/ hypergrid/src/

# Restore and publish the Node project
WORKDIR /src/hypergrid/src/Ouroboros.Hypergrid.Node
RUN dotnet restore
RUN dotnet publish -c Release -o /app --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 9500
ENV ASPNETCORE_URLS=http://+:9500

HEALTHCHECK --interval=15s --timeout=5s --retries=3 \
    CMD curl -f http://localhost:9500/api/health || exit 1

ENTRYPOINT ["dotnet", "Ouroboros.Hypergrid.Node.dll"]
