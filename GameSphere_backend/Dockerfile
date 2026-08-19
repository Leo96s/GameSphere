FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /src

COPY GameSphere_backend.csproj ./
RUN dotnet restore

COPY . ./

FROM base AS development
EXPOSE 8080
ENTRYPOINT ["dotnet", "watch", "--project", "GameSphere_backend.csproj", "run", "--urls", "http://0.0.0.0:8080"]

FROM base AS build
RUN dotnet publish GameSphere_backend.csproj -c Release --no-restore -o /app/publish

FROM base AS migration
RUN dotnet build GameSphere_backend.csproj -c Release --no-restore
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.2
ENV PATH="/tools:${PATH}"
ENTRYPOINT ["dotnet-ef", "database", "update", "--no-build", "--configuration", "Release", "--project", "GameSphere_backend.csproj"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "GameSphere_backend.dll"]
