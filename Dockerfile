FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj
COPY backend_manage/backend_manage/backend_manage.csproj backend_manage/
COPY backend_manage/backend_manage.core/backend_manage.core.csproj backend_manage.core/
COPY backend_manage/backend_manage.shared/backend_manage.shared.csproj backend_manage.shared/

RUN dotnet restore backend_manage/backend_manage.csproj

# Copy full source
COPY backend_manage/backend_manage/ backend_manage/
COPY backend_manage/backend_manage.core/ backend_manage.core/
COPY backend_manage/backend_manage.shared/ backend_manage.shared/

WORKDIR /src/backend_manage
RUN dotnet publish backend_manage.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "backend_manage.dll"]
