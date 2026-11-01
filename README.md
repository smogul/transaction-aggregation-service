# Transaction Aggregation Service

A .NET 8 API that collects a customer's transactions from five mock data sources, puts each transaction into a category, and keeps account statements as PDF files with download links that expire.

Development needs no login. Production uses FAPI, so every call except the health check needs an access token from an authorization server.

All data lives in memory, so it starts fresh from the seed data every time the app starts.

## Run with Docker

Build the image once:

```
docker build -t transaction-aggregation-api .
```

Run in Development:

```
docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development transaction-aggregation-api
```

Then open http://localhost:8080/swagger

Run in Production:

```
docker run -p 8081:8080 -e ASPNETCORE_ENVIRONMENT=Production transaction-aggregation-api
```

Then open http://localhost:8081/swagger

To point Production at your own authorization server, add these two settings to the run command:

```
-e Fapi__Authority=https://your-auth-server/ -e Fapi__TokenUrl=https://your-auth-server/connect/token
```

## Run with Docker Compose

```
docker compose up api-development
docker compose up api-production
```

Development runs on port 8080 and Production on port 8081.

## Run without Docker

```
dotnet run --project transaction-aggregation-api --launch-profile http
dotnet run --project transaction-aggregation-api --launch-profile Production
```

## Try it

Use one of these account numbers: 1000000001, 1000000002 or 2000000001.

The endpoints are:

- GET /api/accounts/{accountId}
- GET /api/accounts/{accountId}/balance
- POST /api/accounts/{accountId}/statements
- POST /api/accounts/{accountId}/statements/download-link
- POST /api/accounts/{accountId}/statements/upload
- GET /api/accounts/{accountId}/statements
- POST /api/statements/{statementId}/download-link
- GET /api/statements/{statementId}/download
- GET /health

The quickest way to get a statement is POST /api/accounts/{accountId}/statements/download-link?fromDate=2026-09-01&toDate=2026-09-30. In Swagger, fill in the fromDate and toDate boxes. It makes a PDF of only the transactions in that period and gives back a download link. Open the link within 15 minutes.

An uploaded statement has no date range. Ask for its link with POST /api/statements/{statementId}/download-link.

## Run the tests

```
dotnet test
```
