FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Quay27-Be.csproj ./
COPY Quay27.Application/Quay27.Application.csproj Quay27.Application/
COPY Quay27.Infrastructure/Quay27.Infrastructure.csproj Quay27.Infrastructure/
COPY Quay27.Domain/Quay27.Domain.csproj Quay27.Domain/
RUN dotnet restore Quay27-Be.csproj

COPY . .
RUN dotnet publish Quay27-Be.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./
COPY Templates ./Templates

ENTRYPOINT ["dotnet", "Quay27-Be.dll"]
