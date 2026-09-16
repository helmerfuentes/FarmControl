FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FarmControlAPI.sln .
COPY FarmControlAPI/FarmControlAPI.csproj FarmControlAPI/
COPY FarmControlAPI.Application/FarmControlAPI.Application.csproj FarmControlAPI.Application/
COPY FarmControlAPI.Domain/FarmControlAPI.Domain.csproj FarmControlAPI.Domain/
COPY FarmControlAPI.Infrastructure/FarmControlAPI.Infrastructure.csproj FarmControlAPI.Infrastructure/
COPY FarmControlAPI.Tests/FarmControlAPI.Tests.csproj FarmControlAPI.Tests/
RUN dotnet restore FarmControlAPI/FarmControlAPI.csproj

COPY . .
RUN dotnet publish FarmControlAPI/FarmControlAPI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
	&& apt-get install -y --no-install-recommends postgresql-client \
	&& rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FarmControlAPI.dll"]
