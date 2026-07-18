FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ApiGateway/Umbral.ApiGateway/*.csproj ./ApiGateway/Umbral.ApiGateway/
RUN cd ApiGateway/Umbral.ApiGateway && dotnet restore

COPY ApiGateway ./ApiGateway
WORKDIR /src/ApiGateway/Umbral.ApiGateway
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Umbral.ApiGateway.dll"]
