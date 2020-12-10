# build
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build
WORKDIR /app/

# copy files from build context to image
COPY ./ObjectHashServer ./ObjectHashServer/
WORKDIR /app/ObjectHashServer

# publish is build + copying required assemblies
RUN dotnet publish ./src/ObjectHashServer.csproj -c Release -o out

##########################
# copy release to runtime
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app

COPY --from=build /app/ObjectHashServer/out ./

ENTRYPOINT ["dotnet", "ObjectHashServer.dll"]
