# Pacman.NET
An Arch Linux package mirror and cache written in C#.

## Features
- Automated Lets Encrypt for SSL certs
- Jwt Security
- Directory browser for local databases
- Create and host package databases
- Automatic mirror downloads

## PLANNED
- IP whitelist
- File signature checking
- Bandwidth limiting for uploads and downloads

### Usage

### Initializing custom repo
Set the following setting in "appsettings.json" to serve the custom repositories that live in that directory. Only contents within that folder will be served. If the path is relative then it will get created in the working directory the app is running from. If it's an absolute path then that path will be used to serve files from. It is up to you to ensure that you have read permissions to any absolute paths that are used.
> "CustomRepoDir": "CustomRepos"


## Misc Details
Database isn't stored, instead it's downloaded and cached on startup. Db requests are proxied over. 
Requires signed databases to be present on the system to initialize. 
Caches the new one if last_sync_time of the server was newer than 2 minutes or whatever configurable amount. 
Parsing of tar allows us to check if the file exists before it makes a request.
Offline directory browser that allows viewing a virtual directory of the db files. Downloads db file and parses it client side.
Db is unzipped with native GZIP compression and the tar contents are parsed to create a virtual directory map. Smoother than a normal
reverse proxy which has to buffer the download into memory before it can transmit to you. Completely offline, and hosted by the server.
Files are transferred and saved as soon as they are received.
Control the rate that files are transferred over with built in bandwidth limiting on both uploads and downloads.

