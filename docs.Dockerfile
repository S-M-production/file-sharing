#Base image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build 
#Creates a directory called src and goes in
WORKDIR /src
#Copys everything into src
COPY . .
#Building files and pulling them in a seperate directory other then src
RUN dotnet tool install -g docfx 
#Adding docfx directory in path so i can run docfx commands
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN docfx metadata 
RUN docfx build 

#Creating a new image
FROM nginx:stable-alpine3.24-perl AS runtime
#Copying the config file
COPY --from=build /src/nginx.conf /etc/nginx/nginx.conf
#Copy the static files from build into /var/www/doc
COPY --from=build /src/_site/ /var/www/doc/
#Need to expose port 80
EXPOSE 80
