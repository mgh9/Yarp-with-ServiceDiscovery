#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Samples/Api1/Api1.csproj", "Samples/Api1/"]
COPY ["ApiGateway.ServiceDiscovery.Abstractions/Yarp.ServiceDiscovery.Abstractions.csproj", "ApiGateway.ServiceDiscovery.Abstractions/"]
COPY ["ApiGateway.ServiceDiscovery.Consul/Yarp.ServiceDiscovery.Consul.csproj", "ApiGateway.ServiceDiscovery.Consul/"]
RUN dotnet restore "./Samples/Api1/./Api1.csproj"
COPY . .
WORKDIR "/src/Samples/Api1"
RUN dotnet build "./Api1.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Api1.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api1.dll"]