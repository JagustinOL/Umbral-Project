FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Umbral.Shared/*.csproj ./Umbral.Shared/
COPY MissionManagement/MissionManagement.WebApi/*.csproj ./MissionManagement/MissionManagement.WebApi/
COPY MissionManagement/MissionManagement.Application/*.csproj ./MissionManagement/MissionManagement.Application/
COPY MissionManagement/MissionManagement.Domain/*.csproj ./MissionManagement/MissionManagement.Domain/
COPY MissionManagement/MissionManagement.Infrastructure/*.csproj ./MissionManagement/MissionManagement.Infrastructure/

RUN cd MissionManagement/MissionManagement.WebApi && dotnet restore

COPY Umbral.Shared ./Umbral.Shared
COPY MissionManagement ./MissionManagement
WORKDIR /src/MissionManagement/MissionManagement.WebApi
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MissionManagement.WebApi.dll"]
