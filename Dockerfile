#Base image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build 
#Creates a directory called src and goes in
WORKDIR /src
#Copys everything into src
COPY . .
#Building files and pulling them in a seperate directory other then src
RUN dotnet publish server-core/server-core.csproj -c Release -o ../app/publish -r linux-x64

#Creating a new image
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
#Creating a app dir and going inside
WORKDIR /app
#Copy the build files from previos image into /app
COPY --from=build /app/publish .
#Need to expose port 13000
EXPOSE 13000
#running the built files, using server public IP and port 13000 as entrance
CMD ["dotnet","server-core.dll","0.0.0.0","13000"]
#CMD ["dotnet","server-core.dll","127.0.0.1","13000"]