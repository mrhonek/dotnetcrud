FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/ASPNETCRUD.API/ASPNETCRUD.API.csproj", "src/ASPNETCRUD.API/"]
COPY ["src/ASPNETCRUD.Application/ASPNETCRUD.Application.csproj", "src/ASPNETCRUD.Application/"]
COPY ["src/ASPNETCRUD.Core/ASPNETCRUD.Core.csproj", "src/ASPNETCRUD.Core/"]
COPY ["src/ASPNETCRUD.Infrastructure/ASPNETCRUD.Infrastructure.csproj", "src/ASPNETCRUD.Infrastructure/"]
RUN dotnet restore "src/ASPNETCRUD.API/ASPNETCRUD.API.csproj"
COPY . .
WORKDIR "/src/ASPNETCRUD.API"
RUN dotnet build "ASPNETCRUD.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ASPNETCRUD.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ASPNETCRUD.API.dll"] 