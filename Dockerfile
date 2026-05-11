# Sử dụng image .NET SDK chính thức để build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy file project và restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy toàn bộ code và build
COPY . ./
RUN dotnet publish -c Release -o out

# Sử dụng image .NET Runtime để chạy
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .

# Mở cổng mà ứng dụng lắng nghe (Render tự gán qua biến môi trường PORT)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

ENTRYPOINT ["dotnet", "Galaxyvibes.API.dll"]