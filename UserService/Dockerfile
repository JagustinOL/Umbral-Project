FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY UserService/UserService.WebApi/*.csproj ./UserService/UserService.WebApi/
COPY UserService/UserService.Application/*.csproj ./UserService/UserService.Application/
COPY UserService/UserService.Domain/*.csproj ./UserService/UserService.Domain/
COPY UserService/UserService.Infrastructure/*.csproj ./UserService/UserService.Infrastructure/

RUN cd UserService/UserService.WebApi && dotnet restore

COPY UserService ./UserService
WORKDIR /src/UserService/UserService.WebApi
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserService.WebApi.dll"]
