import os
import mariadb
from dotenv import load_dotenv

load_dotenv()

filename = 'guest_names.txt'

database_connection = {
  "user" : os.environ["DATABASE_USER"],
  "password" : os.environ["DATABASE_PASSWORD"],
  "host" : os.environ["DATABASE_HOST"],
  "port" : int(os.environ["DATABASE_PORT"]),
  "database" : os.environ["DATABASE_NAME"]
}

dconn = mariadb.connect(**database_connection)
cursor = dconn.cursor()
cursor.execute("SELECT * FROM guestnames")
rows = cursor.fetchall()

names_dict = {}

for row in rows:
    names_dict[row[0]] = row[0]

names_from_file = []

with open(filename) as file_object:
    lines = file_object.readlines()
    # split each line into a list
    for line in lines:
        words = line.split()
        if len(words) < 1:
            continue
        name1 = words[0]
        names_dict[name1] = name1

cursor.execute("TRUNCATE TABLE guestnames")

# loop through the dictionary and print the names
for name in names_dict:
    print(name)
    # insert the name into guestnames table
    #cursor.execute("INSERT INTO guestnames (name) VALUES (?)", (name,))




