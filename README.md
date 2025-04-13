# ASP.NET Core CRUD API with Clean Architecture

A RESTful API built with ASP.NET Core and Entity Framework Core using Clean Architecture principles. This project demonstrates how to create a well-structured API that follows best practices.

## Features

- **Clean Architecture** pattern
- **Entity Framework Core** with PostgreSQL
- **JWT Authentication** with refresh tokens
- **Repository and Unit of Work** patterns
- **AutoMapper** for object mapping
- **FluentValidation** for request validation
- **Swagger UI** with JWT support
- **Global Exception Handling**
- **Docker & Docker Compose** support
- **Railway** deployment ready

## Project Structure

The solution follows Clean Architecture and is organized into four main projects:

- **ASPNETCRUD.Core**: Domain entities, interfaces, and exceptions
- **ASPNETCRUD.Application**: DTOs, services, validators, and mappings
- **ASPNETCRUD.Infrastructure**: Database, repositories, and external services
- **ASPNETCRUD.API**: Controllers, middleware, and configuration

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose (optional, for containerized development)
- Railway account (optional, for deployment)

### Running Locally

1. Clone the repository
   ```
   git clone https://github.com/yourusername/aspnetcrud.git
   cd aspnetcrud
   ```

2. Run with Docker Compose (recommended)
   ```
   docker-compose up -d
   ```
   This will start the API, PostgreSQL database, and pgAdmin.
   - API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger
   - pgAdmin: http://localhost:5050 (Email: admin@aspnetcrud.com, Password: admin)

3. Or run locally without Docker
   - Install PostgreSQL and update connection string in appsettings.json
   - Run the API
     ```
     cd src/ASPNETCRUD.API
     dotnet run
     ```

## API Endpoints

### Authentication

- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Login and get JWT token
- `POST /api/auth/refresh-token` - Refresh JWT token
- `POST /api/auth/revoke` - Revoke refresh token

### Products

- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by id
- `GET /api/products/category/{categoryId}` - Get products by category
- `POST /api/products` - Create a new product (Admin role required)
- `PUT /api/products/{id}` - Update product (Admin role required)
- `DELETE /api/products/{id}` - Delete product (Admin role required)

### Categories

- `GET /api/categories` - Get all categories
- `GET /api/categories/{id}` - Get category by id
- `POST /api/categories` - Create a new category (Admin role required)
- `PUT /api/categories/{id}` - Update category (Admin role required)
- `DELETE /api/categories/{id}` - Delete category (Admin role required)

## Security Considerations

Before deploying to production, make sure to:

1. **Update JWT Settings**: 
   - Replace the placeholder JWT key in `appsettings.json` with a strong, randomly generated key
   - Use environment variables instead of configuration files for secrets in production
   - `JwtSettings__Key` should be at least 256 bits (32 characters) of randomness

2. **Database Security**:
   - Don't use the default `postgres` username/password in production
   - Create a dedicated database user with minimal required permissions
   - Use environment variables for database connection strings

3. **Production Security Checklist**:
   - Enable HTTPS and configure proper certificates
   - Review and test authorization policies
   - Implement rate limiting for APIs
   - Consider implementing API keys for non-user based access

## Deployment to Railway

1. Create a new Railway project
2. Add a PostgreSQL plugin
3. Connect your GitHub repository
4. Configure environment variables:
   - `ASPNETCORE_ENVIRONMENT` = Production
   - `ConnectionStrings__DefaultConnection` = (Use the Railway PostgreSQL connection string)
   - `JwtSettings__Key` = (Your secure key - use a random generator)
   - `JwtSettings__Issuer` = (Your issuer)
   - `JwtSettings__Audience` = (Your audience)
   - `JwtSettings__DurationInMinutes` = 60

5. Deploy and enjoy!

## License

This project is licensed under the MIT License - see the LICENSE file for details. 