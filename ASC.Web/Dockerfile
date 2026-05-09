# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ASC.Solution.slnx ./
COPY ASC.Web/ASC.Web.csproj ASC.Web/
COPY ASC.business/ASC.business.csproj ASC.business/
COPY ASC.DataAccess/ASC.DataAccess.csproj ASC.DataAccess/
COPY ASC.Model/ASC.Model.csproj ASC.Model/
COPY ASC.Utilities/ASC.Utilities.csproj ASC.Utilities/
COPY ASC.Tests/ASC.Tests.csproj ASC.Tests/

RUN dotnet restore ASC.Web/ASC.Web.csproj

COPY . .
RUN dotnet publish ASC.Web/ASC.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ASC.Web.dll"]
