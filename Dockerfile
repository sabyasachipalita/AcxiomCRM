FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/AcxiomCRM/AcxiomCRM.csproj", "src/AcxiomCRM/"]
RUN dotnet restore "src/AcxiomCRM/AcxiomCRM.csproj"

COPY . .
WORKDIR "/src/src/AcxiomCRM"
RUN dotnet build "AcxiomCRM.csproj" -c Release -o /app/build
RUN dotnet publish "AcxiomCRM.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AcxiomCRM.dll"]
