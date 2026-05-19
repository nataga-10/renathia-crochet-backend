# 🧶 Renathia Crochet — Backend API

API REST para la plataforma e-commerce de crochet artesanal.  
Desarrollada con **ASP.NET Core 9** siguiendo Clean Architecture.

**[Ver Swagger UI](https://renathia-api.azurewebsites.net/swagger)**

## Stack
ASP.NET Core 9 · Entity Framework Core · Azure SQL · JWT · SendGrid · Wompi

## Correr localmente
```bash
dotnet run --project src/RenathiaCrochet.API
```

## Variables de entorno
Usar User Secrets de .NET:
```bash
dotnet user-secrets set "JWT_SECRET" "tu-clave"
dotnet user-secrets set "SENDGRID_API_KEY" "SG.xxx"
```

## Repositorio Frontend
[renathia-crochet-frontend](https://github.com/nataga-10/renathia-crochet-frontend)
