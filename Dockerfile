FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app

EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["SmartWMS.Api/SmartWMS.Api.csproj", "SmartWMS.Api/"]

RUN dotnet restore "SmartWMS.Api/SmartWMS.Api.csproj"

COPY . .

WORKDIR "/src/SmartWMS.Api"

RUN dotnet build "SmartWMS.Api.csproj" \
    -c Release \
    -o /app/build

FROM build AS publish

RUN dotnet publish "SmartWMS.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SmartWMS.Api.dll"]