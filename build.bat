@echo off

ECHO Ultimate building tool 30000 by dyzoon.dev
ECHO Dotnet SDK required for building. Get it at https://dotnet.microsoft.com/en-us/download/dotnet/7.0

del /q "bld"
mkdir "bld"
dotnet publish -c Release -p:AssemblyName=app -o "bld"
ECHO FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine-arm64v8 >> bld/Dockerfile
ECHO WORKDIR /app >> bld/Dockerfile
ECHO COPY . ./ >> bld/Dockerfile
ECHO ENTRYPOINT ["dotnet", "app.dll"] >> bld/Dockerfile
ECHO Don't forget to move congifuration files out of source directory! (docker -v conflict possibility)