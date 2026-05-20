FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["TeamService.Domain/TeamService.Domain.csproj", "TeamService.Domain/"]
COPY ["TeamService.Application/TeamService.Application.csproj", "TeamService.Application/"]
COPY ["TeamService.Infrastructure/TeamService.Infrastructure.csproj", "TeamService.Infrastructure/"]
COPY ["TeamService.WebApi/TeamService.WebApi.csproj", "TeamService.WebApi/"]

RUN dotnet restore "TeamService.WebApi/TeamService.WebApi.csproj"

COPY . .
WORKDIR /src/TeamService.WebApi
RUN dotnet publish "TeamService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TeamService.WebApi.dll"]
