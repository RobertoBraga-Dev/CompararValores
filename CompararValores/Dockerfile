#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS base
RUN apt-get update && apt-get install -y libgdiplus

WORKDIR /app
FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["CompararValores/CompararValores.csproj", "CompararValores/"]
RUN dotnet restore "CompararValores/CompararValores.csproj"
COPY . .
WORKDIR "/src/CompararValores"
RUN dotnet build "CompararValores.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CompararValores.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CompararValores.dll"]