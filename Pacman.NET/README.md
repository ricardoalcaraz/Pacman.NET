# Planned features
 - Hosting of cache
 - PGP signature signing
 - Package uploads
 - database creation
 - rate limiting
 - ip whitelist
 - Cache done
PGP sig check TODO
Uploads High Priority
Database creation TODO
Database parsing DONE
Bandwidth Limiting In Progress
Ip whitelist WILL NOT DO
Concurrent user limiting TODO
Mirror sync In Progress
Reverse Proxy DONE


Creating custom repos requires pacman with the signing key inititialized
All packages must be signed with a key controlled by the server. The server repo key is signed by local pacman installation so packages are trusted. External users will need to import the key from /pub
Home Dir:/etc/pacnet

# Usage
Hosting as a cache server is done by initializing systemctl service

    systemctl enable --now Pacman.NET

The default settings use pacman.conf. 

## Creating a custom repo
    Pacman.NET custom_repo_name

Publishing a package will return the signature of the package
Packages that heven't been signed will be staged until trusted signature is present
```bash
    Pacman.NET publish /path/to/file /path/to/file2 ...
    Pacman.NET sign --pending
    Pacman.NET sign --all
    Pacman.NET sign -s key /path/to/file
    # Choose one of the following
    # 1.... repo_name
    
    #--repo-name {name} Create a repo if it does not exist. Must be
    #--create-repo {name} Specify repo name
    
    # default repo name for root is {Host}.db
    # default repo for user is {User}.db
    
    # Uses default gpg key to sign
    # specify custom signing key
    
    # Specify specific repo will create it if it doesn't exist
    Pacman.NET publish --create --repo_name repo_name /path/to/file
    # Create new repo[Y/n]
    
    Pacman.NET --key-dir /etc/Pacman.NET/gnupg -r custom_repo_name
```
It will create a custom repository and add the signing key to the pacman key chain

pacman-key --lsign-key keyId
Builds a local web of trust by generating a key. Key must be added to pacman key ring to trust packages
