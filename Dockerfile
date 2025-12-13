FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN dotnet tool install --global dotnet-ef --version 8.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"
WORKDIR /src
COPY ["ProjectService.csproj", "./"]
RUN dotnet restore "./ProjectService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ProjectService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProjectService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProjectService.dll"]
