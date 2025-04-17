FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/ASPNETCRUD.API/ASPNETCRUD.API.csproj", "ASPNETCRUD.API/"]
COPY ["src/ASPNETCRUD.Application/ASPNETCRUD.Application.csproj", "ASPNETCRUD.Application/"]
COPY ["src/ASPNETCRUD.Core/ASPNETCRUD.Core.csproj", "ASPNETCRUD.Core/"]
COPY ["src/ASPNETCRUD.Infrastructure/ASPNETCRUD.Infrastructure.csproj", "ASPNETCRUD.Infrastructure/"]
RUN dotnet restore "ASPNETCRUD.API/ASPNETCRUD.API.csproj"
COPY ["src/ASPNETCRUD.API/", "ASPNETCRUD.API/"]
COPY ["src/ASPNETCRUD.Application/", "ASPNETCRUD.Application/"]
COPY ["src/ASPNETCRUD.Core/", "ASPNETCRUD.Core/"]
COPY ["src/ASPNETCRUD.Infrastructure/", "ASPNETCRUD.Infrastructure/"]
WORKDIR "/src/ASPNETCRUD.API"
RUN dotnet build "ASPNETCRUD.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ASPNETCRUD.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for API calls
RUN apt-get update && apt-get install -y curl && apt-get clean

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8000
ENV PORT=8000
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose the port
EXPOSE 8000
EXPOSE 80

# Create health check file
RUN echo "Healthy" > /app/health.txt

# Start the application directly
ENTRYPOINT ["dotnet", "ASPNETCRUD.API.dll"] 