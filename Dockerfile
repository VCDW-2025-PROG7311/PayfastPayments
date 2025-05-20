# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the .csproj file first to restore dependencies
COPY ["PayFastPayments/PayFastPayments.csproj", "PayFastPayments/"]

# Restore the dependencies for the project
RUN dotnet restore "PayFastPayments/PayFastPayments.csproj"

# Now copy the rest of the application files
COPY . .

# Set the working directory to the PayFastPayments folder
WORKDIR "/src/PayFastPayments"

# Build the project
RUN dotnet build "PayFastPayments.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "PayFastPayments.csproj" -c Release -o /app/publish

# Use the official .NET ASP.NET image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

# Copy the published files from the build stage
COPY --from=publish /app/publish .

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "PayFastPayments.dll"]
