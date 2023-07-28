FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:7.0-alpine as build
WORKDIR /build
COPY . .
RUN dotnet publish -c Release -p:AssemblyName=app -o "output"
WORKDIR /build/output
RUN rm configuration.json

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine-arm64v8
WORKDIR /app 
RUN apk add icu
COPY --from=build . ./ 

ENTRYPOINT ["dotnet", "app.dll"] 
