FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["PayFastPayments/PayFastPayments.csproj", "PayFastPayments/"]

RUN dotnet restore "PayFastPayments/PayFastPayments.csproj"

COPY . .

WORKDIR "/src/PayFastPayments"

RUN dotnet build "PayFastPayments.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PayFastPayments.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PayFastPayments.dll"]
