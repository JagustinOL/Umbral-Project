FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MissionManagement.WebApi/*.csproj ./MissionManagement.WebApi/
COPY MissionManagement.Application/*.csproj ./MissionManagement.Application/
COPY MissionManagement.Domain/*.csproj ./MissionManagement.Domain/
COPY MissionManagement.Infrastructure/*.csproj ./MissionManagement.Infrastructure/

RUN cd MissionManagement.WebApi && dotnet restore

COPY . .
WORKDIR "/src/MissionManagement.WebApi"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MissionManagement.WebApi.dll"]
