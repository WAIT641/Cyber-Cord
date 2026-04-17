FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Cyber-Cord.Api/Cyber-Cord.Api.csproj", "Cyber-Cord.Api/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Cyber-Cord.Api/Cyber-Cord.Api.csproj"
COPY . .
WORKDIR "/src/Cyber-Cord.Api"
RUN dotnet build "./Cyber-Cord.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cyber-Cord.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cyber-Cord.Api.dll"]
