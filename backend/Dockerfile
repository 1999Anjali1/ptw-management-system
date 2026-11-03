# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so this layer is cached when only code changes
COPY src/PtwSystem.Api/PtwSystem.Api.csproj src/PtwSystem.Api/
RUN dotnet restore src/PtwSystem.Api/PtwSystem.Api.csproj

COPY src/ src/
RUN dotnet publish src/PtwSystem.Api/PtwSystem.Api.csproj -c Release -o /app --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "PtwSystem.Api.dll"]
