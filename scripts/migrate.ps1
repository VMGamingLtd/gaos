cd ..
#dotnet ef database drop
mysql -uroot -proot -e 'drop database gaos;create database gaos;'
rm -r -force Migrations
if (Test-Path -Path Migrations) {
	Remove-Item -Path Migrations -Recurse -Force
} 
dotnet ef migrations add InitialCreate
dotnet ef database update
