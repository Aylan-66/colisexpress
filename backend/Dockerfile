FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY global.json ./
COPY ColisExpress.sln ./
COPY src/ColisExpress.Domain/ColisExpress.Domain.csproj src/ColisExpress.Domain/
COPY src/ColisExpress.Application/ColisExpress.Application.csproj src/ColisExpress.Application/
COPY src/ColisExpress.Infrastructure/ColisExpress.Infrastructure.csproj src/ColisExpress.Infrastructure/
COPY src/ColisExpress.Web/ColisExpress.Web.csproj src/ColisExpress.Web/
RUN dotnet restore src/ColisExpress.Web/ColisExpress.Web.csproj

COPY src/ src/
RUN dotnet publish src/ColisExpress.Web/ColisExpress.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080
ENTRYPOINT ["dotnet", "ColisExpress.Web.dll"]
