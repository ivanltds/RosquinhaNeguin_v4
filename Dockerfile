# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj e restaurar dependências
COPY ["RosquinhaNeguin.csproj", "./"]
RUN dotnet restore

# Copiar o restante dos arquivos e buildar
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Estágio de Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expor a porta 8080 (padrão do .NET 8 no Linux)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "RosquinhaNeguin.dll"]
