# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore packages first so Docker can cache them.
COPY PROG2026_part2.csproj ./
RUN dotnet restore

# Copy the source (tests/ is excluded by .dockerignore) and publish.
COPY . .
RUN dotnet publish PROG2026_part2.csproj -c Release -o /app/publish --no-restore

# ---------- Stage 2: run ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "PROG2026_part2.dll"]
