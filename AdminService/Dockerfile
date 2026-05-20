FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AdminService.Domain/AdminService.Domain.csproj", "AdminService.Domain/"]
COPY ["AdminService.Application/AdminService.Application.csproj", "AdminService.Application/"]
COPY ["AdminService.Infrastructure/AdminService.Infrastructure.csproj", "AdminService.Infrastructure/"]
COPY ["AdminService.WebApi/AdminService.WebApi.csproj", "AdminService.WebApi/"]

RUN dotnet restore "AdminService.WebApi/AdminService.WebApi.csproj"

COPY . .
WORKDIR /src/AdminService.WebApi
RUN dotnet publish "AdminService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AdminService.WebApi.dll"]
