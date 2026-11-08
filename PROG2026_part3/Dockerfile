# ---------- Stage 1: build ----------
# The SDK image has everything needed to compile the app (it is large, so it is NOT used to run it).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy ONLY the project file first and restore packages.
# Docker caches this layer, so packages are not downloaded again unless the .csproj changes.
COPY PROG2026_part3.csproj ./
RUN dotnet restore

# Now copy the rest of the source code and publish a Release build.
COPY . .
RUN dotnet publish PROG2026_part3.csproj -c Release -o /app/publish --no-restore

# ---------- Stage 2: run ----------
# The small ASP.NET runtime image: no SDK, so the final image is much smaller and safer.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The app listens on port 8080 inside the container.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Run as the built-in non-root user instead of root (security best practice).
USER $APP_UID

ENTRYPOINT ["dotnet", "PROG2026_part3.dll"]
