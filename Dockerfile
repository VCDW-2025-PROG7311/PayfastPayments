# Use the official .NET SDK image as the build environment
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy only the .csproj files to take advantage of Docker caching
COPY ["PayFastPayments.csproj", "/"]

# Restore the dependencies for the project
RUN dotnet restore "/PayFastPayments.csproj"

# Now copy the rest of the application files
COPY . .

# Set the working directory to the PayFastPayments folder
WORKDIR "/src/PayFastPayments"

# Build the project
RUN dotnet build "PayFastPayments.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "PayFastPayments.csproj" -c Release -o /app/publish

# Final image with the built application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PayFastPayments.dll"]
