# 🚀 VayaPreguntita API

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Supabase](https://img.shields.io/badge/Supabase-3ECF8E?style=for-the-badge&logo=supabase&logoColor=white)

> This document provides step-by-step instructions to set up the local development environment for the API from scratch.

## 🛠️ 1. Prerequisites (System Requirements)

Before cloning the repository, ensure your machine has the following installed:

- **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)**: The core framework.
- **Code Editor**: VS Code with the **C# Dev Kit** extension.
- **Entity Framework Core Tools**: A global tool required for database migrations. Install it by running the following command in your terminal:

```bash
dotnet tool install --global dotnet-ef
```

## 📦 2. Tech Stack & Key Packages

The `.csproj` file already includes the following essential dependencies:

- `Npgsql.EntityFrameworkCore.PostgreSQL`: The provider that allows EF Core to communicate with the PostgreSQL database.
- `Microsoft.EntityFrameworkCore.Design`: Design-time tools necessary to generate and apply code-first migrations.
- `AutoMapper`: Used for convention-based object-object mapping between Entities and DTOs.

## ⚙️ 3. Getting Started (Local Setup)

**Step 1: Clone the repository**
Download the source code and navigate to the API directory:

```bash
git clone <your-repo-url>
cd VayaPreguntita/server/VayaPreguntita.API
```

**Step 2: Restore dependencies**
Download all the packages listed in the project file:

```bash
dotnet restore
```

**Step 3: Configure Local Secrets (Crucial!)**
For security reasons, the Supabase database password is not tracked in version control. You must link the connection string locally using the `.csproj` UserSecretsId. Run this command:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_SUPABASE_CONNECTION_STRING_HERE"
```

**Step 4: Synchronize the Database**
Apply any pending migrations to build or update the database schema:

```bash
dotnet ef database update
```

**Step 5: Run the server**
Start the API in development mode:

```bash
dotnet run
```
