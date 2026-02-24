FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Gcpe.Hub.API.sln ./
COPY Gcpe.Hub.API/Gcpe.Hub.API.csproj Gcpe.Hub.API/
COPY Gcpe.Hub.API.Tests/Gcpe.Hub.API.Tests.csproj Gcpe.Hub.API.Tests/
COPY Gcpe.Hub.API.IntegrationTests/Gcpe.Hub.API.IntegrationTests.csproj Gcpe.Hub.API.IntegrationTests/

RUN dotnet restore Gcpe.Hub.API.sln

COPY . ./
RUN dotnet build Gcpe.Hub.API.sln -c Release -o /app/build
RUN dotnet publish Gcpe.Hub.API/Gcpe.Hub.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# OpenShift runs containers with an arbitrary UID. Ensure /app is writable.
RUN chgrp -R 0 /app && chmod -R g=u /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Gcpe.Hub.API.dll"]
