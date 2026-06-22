FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SessionManagement/SessionManagement.WebApi/*.csproj ./SessionManagement/SessionManagement.WebApi/
COPY SessionManagement/SessionManagement.Infrastructure/*.csproj ./SessionManagement/SessionManagement.Infrastructure/
COPY SessionManagement/SessionManagement.Application/*.csproj ./SessionManagement/SessionManagement.Application/
COPY SessionManagement/SessionManagement.Domain/*.csproj ./SessionManagement/SessionManagement.Domain/

RUN cd SessionManagement/SessionManagement.WebApi && dotnet restore

COPY SessionManagement ./SessionManagement
WORKDIR /src/SessionManagement/SessionManagement.WebApi
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SessionManagement.WebApi.dll"]
