#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ENV TZ=Asia/Taipei
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
WORKDIR /app
ENV ASPNETCORE_URLS=https://+:443;http://+:80
EXPOSE 443
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["source/services/Storage/Storage.csproj", "services/Storage/"]
COPY ["source/schema/Data/Data.csproj", "schema/data/"]
RUN dotnet restore "services/Storage/Storage.csproj"
COPY source .
WORKDIR "/src/services/Storage"
RUN dotnet build "Storage.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Storage.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmptyProject.Services.Storage.dll"]