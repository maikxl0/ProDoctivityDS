FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ProDoctivityDS/ProDoctivityDS.csproj", "ProDoctivityDS/"]
COPY ["ProDoctivityDS.Application/ProDoctivityDS.Application.csproj", "ProDoctivityDS.Application/"]
COPY ["ProDoctivityDS.Domain/ProDoctivityDS.Domain.csproj", "ProDoctivityDS.Domain/"]
COPY ["ProDoctivityDS.Persistence/ProDoctivityDS.Persistence.csproj", "ProDoctivityDS.Persistence/"]
COPY ["ProDoctivityDS.Shared/ProDoctivityDS.Shared.csproj", "ProDoctivityDS.Shared/"]

RUN dotnet restore "ProDoctivityDS/ProDoctivityDS.csproj"

COPY . .
RUN dotnet publish "ProDoctivityDS/ProDoctivityDS.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV APP_DATA_DIR=/var/data

COPY --from=build /app/publish .
COPY docker-entrypoint.sh /app/docker-entrypoint.sh

RUN chmod +x /app/docker-entrypoint.sh

EXPOSE 10000

ENTRYPOINT ["/app/docker-entrypoint.sh"]
