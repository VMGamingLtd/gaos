# Database

See [https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli] (https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli)
See [https://learn.microsoft.com/en-us/ef/core/cli/dotnet] (https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

## Drop Existing Database

```
> dotnet ef database drop
```

## Migrate Database

```
> rm -r Migrations
> dotnet ef migrations add InitialCreate
> dotnet ef database update
```

# Run

```
> dotnet run --launch-profile https
```

or

```
$Env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --no-launch-profile --urls="https://localhost:7070"
```


# Build Release

```
> export ASPNETCORE_ENVIRONMENT=Test
> dotnet publish --configuration Release
> chown -R gaos:gaos /opt/gaos/bin/Release/net8.0/publish
```

# Init database on test server
```
> systemctl stop gaos.service
> export ASPNETCORE_ENVIRONMENT=Test
> cd scripts
> ./migrate.sh
> systemctl start gaos.service
```