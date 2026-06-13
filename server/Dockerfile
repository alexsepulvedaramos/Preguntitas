# Use the ASP.NET Core runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# Expose the default port
EXPOSE 8080

# Use the .NET SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the API project file to restore dependencies
# This caches the restore step unless the .csproj changes
COPY ["VayaPreguntita.API/VayaPreguntita.API.csproj", "VayaPreguntita.API/"]
RUN dotnet restore "VayaPreguntita.API/VayaPreguntita.API.csproj"

# Copy the entire server source code
COPY . .

# Change working directory to the API folder and build
WORKDIR "/src/VayaPreguntita.API"
RUN dotnet build "VayaPreguntita.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "VayaPreguntita.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage: copy the published app and define the entry point
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VayaPreguntita.API.dll"]