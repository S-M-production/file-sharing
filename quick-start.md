# Quick Start

0. Pre-requisites:
   1. When you are developing or coding using this its better to have git installed. [Git Tutorial](https://www.w3schools.com/git/)
   2. You also need .NET 10 SDK to develop or use this code. [.NET 10 SDK downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
   3. Having docker installed is not nessicary, but good to have if you want to deploy the server. [Docker Desktop install](https://docs.docker.com/desktop/)  


1. Creating and opening the repo
     1. clone the repo `clone https://github.com/S-M-production/file-sharing`
     2. go into the repo `cd file-sharing`  


2. running the files:
    - to run client app do:
      - `dotnet run --project ./src/main/client-ui/` (linux)
      - `dotnet run --project .\src\main\client-ui\` (windows)
    - to run server app do (127.0.0.1 is local host, and 13000 is our default port to host on):
      - `dotnet run --project ./src/main/server-core/ 127.0.0.1 13000` (linux)
      - `dotnet run --project .\src\main\server-core\ 127.0.0.1 13000` (windows)
      - using docker (Have docker engine or desktop installed already):
        -  `docker run -d -p 13000:13000 --name file-sharing-server munna00/file-sharing-server:latest`