FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BigEcommerce.sln ./
COPY Ecommerce.Api/Ecommerce.Api.csproj Ecommerce.Api/
COPY Ecommerce.Tests/Ecommerce.Tests.csproj Ecommerce.Tests/
RUN dotnet restore BigEcommerce.sln

COPY . .
RUN dotnet publish Ecommerce.Api/Ecommerce.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN mkdir -p /app/data && chown -R app:app /app
COPY --from=build /app/publish .
USER app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Ecommerce.Api.dll"]
