# Stage 1: Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

# Thêm arg cho connection string
ARG ConnectionStrings__DefaultConnection
ENV ConnectionStrings__DefaultConnection=${ConnectionStrings__DefaultConnection}

WORKDIR /src

# COPY các csproj đúng đường dẫn từ context HutechQuiz
COPY ["backend_manage/backend_manage/backend_manage.csproj", "backend_manage/"]
COPY ["backend_manage/backend_manage.core/backend_manage.core.csproj", "backend_manage.core/"]
COPY ["backend_manage/backend_manage.shared/backend_manage.shared.csproj", "backend_manage.shared/"]

# Restore packages
RUN dotnet restore "backend_manage/backend_manage.csproj"

# COPY toàn bộ source code
COPY backend_manage/backend_manage/ ./backend_manage/
COPY backend_manage/backend_manage.core/ ./backend_manage.core/
COPY backend_manage/backend_manage.shared/ ./backend_manage.shared/

# Build project
RUN dotnet build "backend_manage/backend_manage.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "backend_manage/backend_manage.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final image
FROM base AS final
WORKDIR /app

# ARG và ENV cho connection string ở final stage
ARG ConnectionStrings__DefaultConnection
ENV ConnectionStrings__DefaultConnection=${ConnectionStrings__DefaultConnection}

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "backend_manage.dll"]
