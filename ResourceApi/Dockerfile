# STAGE 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ResourceApi/ResourceApi.csproj", "ResourceApi/"]
RUN dotnet restore "ResourceApi/ResourceApi.csproj"
COPY . .
WORKDIR "/src/ResourceApi"
RUN dotnet build "ResourceApi.csproj" -c Release -o /app/build

# STAGE 2: Publish
FROM build AS publish
RUN dotnet publish "ResourceApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 3: Final Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "ResourceApi.dll"]
