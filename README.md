**YARP with Service Discovery**

This repository demonstrates how to build an API Gateway using YARP (Yet Another Reverse Proxy) with dynamic service discovery support (e.g., via Consul).
The architecture is designed for microservices-based systems that require runtime routing flexibility, scalable configuration, and clean separation of concerns.


**Highlights**
YARP (Yet Another Reverse Proxy) used as a reverse-proxy/API gateway.
Service discovery integration: resolves route destinations dynamically.
Demonstrates code-first configuration for microservice registration and proxying.
Self-contained Docker + .NET 8 solution — no additional tools required.


**Features**
Reverse proxy with YARP for routing incoming traffic to backend services.
Service discovery integration to resolve destinations at runtime.
Code-first configuration for easy customization and minimal setup.
Dockerized environment using .NET 8 — simple to run and extend.
Minimal boilerplate — focused on showcasing integration and extensibility.

**Project Structure**
/Gateway
  └── Program.cs          // YARP setup and service discovery
  └── yarp.json           // Optional YARP configuration (not required in this code-first setup)

  └── ServiceDiscovery/
      └── ConsulServiceDiscoveryProvider.cs
      └── IDiscoveryProvider.cs
      └── StaticServiceDiscoveryProvider.cs (example fallback)

  └── Controllers/        // Optional: for gateway health checks
  └── Dockerfile

/Services
  └── SampleService1/
  └── SampleService2/
  ...



**How to Run**
**Prerequisites**
.NET 8 SDK
Docker (for running services and gateway)
Consul (optional, if enabled for discovery)

**Running with Docker Compose**
A docker-compose.yml file can be added to orchestrate Gateway + Service + Consul containers. You can extend this project with that setup.

**Manual Run**
Run sample backend services:
dotnet run --project ./Services/SampleService1
dotnet run --project ./Services/SampleService2

Run the gateway:
dotnet run --project ./Gateway


**How It Works**
At startup, the Gateway loads registered services via IDiscoveryProvider.
YARP routes are configured programmatically based on these services.
Incoming HTTP requests are reverse proxied to backend services resolved via the discovery mechanism.
You can switch between static, file-based, or Consul-based providers with minimal changes.


**Notes**
This project avoids static appsettings.json or yarp.json files to promote runtime flexibility.
For production-grade scenarios, consider adding health checks, retries, circuit breakers, and proper observability.
