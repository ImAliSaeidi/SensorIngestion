ARG DOTNET_SDK_IMAGE=mcr.hamdocker.ir/dotnet/sdk:10.0.400
ARG DOTNET_RUNTIME_IMAGE=mcr.hamdocker.ir/dotnet/aspnet:10.0.12

FROM ${DOTNET_SDK_IMAGE} AS build
ARG NUGET_PACKAGES_SOURCE=https://repo.hmirror.ir/nuget
WORKDIR /src

COPY SensorIngestion.slnx ./
COPY src/SensorIngestion.Api/SensorIngestion.Api.csproj src/SensorIngestion.Api/
COPY src/SensorIngestion.Application/SensorIngestion.Application.csproj src/SensorIngestion.Application/
COPY src/SensorIngestion.Domain/SensorIngestion.Domain.csproj src/SensorIngestion.Domain/
COPY src/SensorIngestion.Infrastructure/SensorIngestion.Infrastructure.csproj src/SensorIngestion.Infrastructure/
COPY tests/SensorIngestion.IntegrationTests/SensorIngestion.IntegrationTests.csproj tests/SensorIngestion.IntegrationTests/
COPY tests/SensorIngestion.UnitTests/SensorIngestion.UnitTests.csproj tests/SensorIngestion.UnitTests/
RUN dotnet restore SensorIngestion.slnx --source ${NUGET_PACKAGES_SOURCE}

COPY . .

FROM build AS test
ENTRYPOINT ["dotnet", "test", "SensorIngestion.slnx", "--configuration", "Release", "--no-restore"]

FROM build AS publish
RUN dotnet publish src/SensorIngestion.Api/SensorIngestion.Api.csproj --configuration Release --no-restore --output /app/publish

FROM ${DOTNET_RUNTIME_IMAGE} AS final
WORKDIR /app
RUN mkdir -p /app/storage && chown app:app /app/storage
COPY --from=publish --chown=app:app /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SensorIngestion.Api.dll"]
