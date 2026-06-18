FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Umbral.Shared/*.csproj ./Umbral.Shared/
COPY ScoringAudit/ScoringAudit.WebApi/*.csproj ./ScoringAudit/ScoringAudit.WebApi/
COPY ScoringAudit/ScoringAudit.Application/*.csproj ./ScoringAudit/ScoringAudit.Application/
COPY ScoringAudit/ScoringAudit.Domain/*.csproj ./ScoringAudit/ScoringAudit.Domain/
COPY ScoringAudit/ScoringAudit.Infrastructure/*.csproj ./ScoringAudit/ScoringAudit.Infrastructure/

RUN cd ScoringAudit/ScoringAudit.WebApi && dotnet restore

COPY Umbral.Shared ./Umbral.Shared
COPY ScoringAudit ./ScoringAudit
WORKDIR /src/ScoringAudit/ScoringAudit.WebApi
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ScoringAudit.WebApi.dll"]
