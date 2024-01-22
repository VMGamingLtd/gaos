cd ..
dotnet ef database drop
rm -r -force Migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
