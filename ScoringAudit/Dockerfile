FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ScoringAudit.WebApi/*.csproj ./ScoringAudit.WebApi/
COPY ScoringAudit.Application/*.csproj ./ScoringAudit.Application/
COPY ScoringAudit.Domain/*.csproj ./ScoringAudit.Domain/
COPY ScoringAudit.Infrastructure/*.csproj ./ScoringAudit.Infrastructure/

RUN cd ScoringAudit.WebApi && dotnet restore

COPY . .
WORKDIR "/src/ScoringAudit.WebApi"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ScoringAudit.WebApi.dll"]
