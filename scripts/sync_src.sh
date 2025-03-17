set -xe
rsync -avz --exclude=.git/ --exclude=obj/ --exclude=bin/ --exclude=py/ --exclude=scripts/ /c/w1/gaos/ root@usrv:/var/www/html/sources/gaos
ssh root@usrv "chown -R www-data:www-data /var/www/html/sources/gaos"
