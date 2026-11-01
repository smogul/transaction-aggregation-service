FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY transaction-aggregation-domain/transaction-aggregation-domain.csproj transaction-aggregation-domain/
COPY transaction-aggregation-app/transaction-aggregation-app.csproj transaction-aggregation-app/
COPY transaction-aggregation-infra/transaction-aggregation-infra.csproj transaction-aggregation-infra/
COPY transaction-aggregation-api/transaction-aggregation-api.csproj transaction-aggregation-api/
RUN dotnet restore transaction-aggregation-api/transaction-aggregation-api.csproj

COPY transaction-aggregation-domain/ transaction-aggregation-domain/
COPY transaction-aggregation-app/ transaction-aggregation-app/
COPY transaction-aggregation-infra/ transaction-aggregation-infra/
COPY transaction-aggregation-api/ transaction-aggregation-api/
RUN dotnet publish transaction-aggregation-api/transaction-aggregation-api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "transaction-aggregation-api.dll"]
