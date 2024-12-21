cd ..
#dotnet ef database drop
$env:DOTNET_ENVIRONMENT = "Development_multi"
mysql -u gaos_multi -h usrv -p --disable-ssl-verify-server-cert -e 'drop database if exists gaos_multi;create database gaos_multi;'
rm -r -force Migrations
if (Test-Path -Path Migrations) {
	Remove-Item -Path Migrations -Recurse -Force
} 
dotnet  ef migrations add InitialCreate
dotnet  ef database update
