FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BE_AI_Tourism/*.csproj ./BE_AI_Tourism/
RUN dotnet restore BE_AI_Tourism/BE_AI_Tourism.csproj

COPY BE_AI_Tourism/ ./BE_AI_Tourism/
RUN dotnet publish BE_AI_Tourism/BE_AI_Tourism.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BE_AI_Tourism.dll"]
