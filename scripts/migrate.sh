set -x
cd ..
dotnet ef database drop
rm -r -f Migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
