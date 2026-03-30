FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false \
    NUGET_ENHANCED_NETWORK_ENABLED=true
WORKDIR /src
COPY ["ProjectService/ProjectService.csproj", "ProjectService/"]
COPY ["FMN.Vault/FMN.Vault.csproj", "FMN.Vault/"]
RUN dotnet restore "ProjectService/ProjectService.csproj"
RUN dotnet tool install --global dotnet-ef --version 10.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"
COPY ProjectService/ ProjectService/
COPY FMN.Vault/ FMN.Vault/
WORKDIR "/src/ProjectService"
RUN dotnet build "ProjectService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProjectService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProjectService.dll"]
