# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy csproj and restore the application only. Tests are restored by CI,
# but are intentionally excluded from the production image build.
COPY AspNetCoreApiStarter/*.csproj ./AspNetCoreApiStarter/
RUN dotnet restore AspNetCoreApiStarter/AspNetCoreApiStarter.csproj

# Copy source and build
COPY AspNetCoreApiStarter/. ./AspNetCoreApiStarter/
WORKDIR /source/AspNetCoreApiStarter
RUN dotnet build -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy published app from build stage
COPY --from=build /app ./

EXPOSE 5070

ENTRYPOINT ["dotnet", "AspNetCoreApiStarter.dll"]
