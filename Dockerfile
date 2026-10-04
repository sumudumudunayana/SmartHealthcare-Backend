FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY SmartHealthcare.API.csproj .

RUN dotnet restore SmartHealthcare.API.csproj

COPY . .

RUN dotnet publish SmartHealthcare.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SmartHealthcare.API.dll"]