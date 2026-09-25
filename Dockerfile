FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CreditoPlataforma.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish CreditoPlataforma.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "dotnet CreditoPlataforma.dll --urls 'http://0.0.0.0:${PORT:-8080}'"]