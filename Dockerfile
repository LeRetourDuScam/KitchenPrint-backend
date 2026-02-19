# CarbonFootPrint (KitchenPrint) — Backend API

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["KitchenPrint-backend/KitchenPrint-backend.csproj", "KitchenPrint-backend/"]
COPY ["KitchenPrint.Models/KitchenPrint.Core.Models.csproj", "KitchenPrint.Models/"]
COPY ["KitchenPrint.Core/KitchenPrint.API.Core.csproj", "KitchenPrint.Core/"]
COPY ["KitchenPrint.Contracts/KitchenPrint.Contracts.csproj", "KitchenPrint.Contracts/"]
COPY ["KitchenPrint.ENTITIES/KitchenPrint.ENTITIES.csproj", "KitchenPrint.ENTITIES/"]

# Restore dependencies
RUN dotnet restore "KitchenPrint-backend/KitchenPrint-backend.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/KitchenPrint-backend"
RUN dotnet publish "KitchenPrint-backend.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "KitchenPrint-backend.dll"]
