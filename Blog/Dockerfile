# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Blog/*.csproj ./Blog/
WORKDIR /src/Blog
RUN dotnet restore

COPY Blog/. ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
# ENV ASPNETCORE_URLS=http://*:10000 <- this line was causing issues just expose port 8080 and things will be fine
EXPOSE 8080
ENTRYPOINT ["dotnet", "Blog.dll"]
