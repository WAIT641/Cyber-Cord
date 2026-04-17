FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Cyber-Cord.App/Cyber-Cord.App.csproj", "Cyber-Cord.App/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Cyber-Cord.App/Cyber-Cord.App.csproj"
COPY . .
WORKDIR "/src/Cyber-Cord.App"
RUN dotnet build "./Cyber-Cord.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cyber-Cord.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cyber-Cord.App.dll"]
