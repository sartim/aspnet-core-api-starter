# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy csproj and restore
COPY *.sln .
COPY AspNetCoreApiStarter/*.csproj ./AspNetCoreApiStarter/
RUN dotnet restore

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
