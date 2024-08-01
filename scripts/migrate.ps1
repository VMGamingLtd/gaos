cd ..
#dotnet ef database drop
mysql -uroot -proot -e 'drop database gaos;create database gaos;'
rm -r -force Migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
