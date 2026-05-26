FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SessionManagement.WebApi/*.csproj ./SessionManagement.WebApi/
COPY SessionManagement.Infrastructure/*.csproj ./SessionManagement.Infrastructure/
COPY SessionManagement.Application/*.csproj ./SessionManagement.Application/
COPY SessionManagement.Domain/*.csproj ./SessionManagement.Domain/

RUN cd SessionManagement.WebApi && dotnet restore

COPY . .
WORKDIR "/src/SessionManagement.WebApi"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SessionManagement.WebApi.dll"]
