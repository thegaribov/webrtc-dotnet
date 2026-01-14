# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["WebRtcConference.csproj", "./"]
RUN dotnet restore "WebRtcConference.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "WebRtcConference.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "WebRtcConference.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:3000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 3000

ENTRYPOINT ["dotnet", "WebRtcConference.dll"]
