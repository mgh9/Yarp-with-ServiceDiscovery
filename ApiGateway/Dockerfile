#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ApiGateway/Yarp.csproj", "ApiGateway/"]
COPY ["ApiGateway.ServiceDiscovery.Abstractions/Yarp.ServiceDiscovery.Abstractions.csproj", "ApiGateway.ServiceDiscovery.Abstractions/"]
COPY ["ApiGateway.ServiceDiscovery.Consul/Yarp.ServiceDiscovery.Consul.csproj", "ApiGateway.ServiceDiscovery.Consul/"]
RUN dotnet restore "./ApiGateway/./Yarp.csproj"
COPY . .
WORKDIR "/src/ApiGateway"
RUN dotnet build "./Yarp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Yarp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Yarp.dll"]