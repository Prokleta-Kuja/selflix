# Development

## Migrations

```
dotnet build
dotnet ef migrations add --no-build -c SqliteDbContext -o ./Entities/Migrations/Sqlite Initial
dotnet ef migrations add --no-build -c PostgresDbContext -o ./Entities/Migrations/Postgres Initial
```

## postgres

```
SELECT *, pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE pid <> pg_backend_pid()
AND datname = 'dev_selflix';

DROP DATABASE dev_selflix;

CREATE DATABASE dev_selflix
    WITH
    OWNER = dev
    ENCODING = 'UTF8'
    LC_COLLATE = 'hr_HR.utf8'
    LC_CTYPE = 'hr_HR.utf8'
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;
```

## OpenAPI

```
npm exec --prefix src/web openapi-typescript-codegen -- --useOptions --input http://localhost:5080/swagger/v1/swagger.json --output ./src/web/src/api
npm exec --prefix src/web openapi-typescript-codegen -- --useOptions --input http://localhost:5080/swagger/v1/swagger.json --output ./src/native/api
```
