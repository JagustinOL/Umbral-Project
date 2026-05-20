FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SessionManagement.Domain/SessionManagement.Domain.csproj", "SessionManagement.Domain/"]
COPY ["SessionManagement.Application/SessionManagement.Application.csproj", "SessionManagement.Application/"]
COPY ["SessionManagement.Infrastructure/SessionManagement.Infrastructure.csproj", "SessionManagement.Infrastructure/"]
COPY ["SessionManagement.WebApi/SessionManagement.WebApi.csproj", "SessionManagement.WebApi/"]

RUN dotnet restore "SessionManagement.WebApi/SessionManagement.WebApi.csproj"

COPY . .
WORKDIR /src/SessionManagement.WebApi
RUN dotnet publish "SessionManagement.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SessionManagement.WebApi.dll"]
