# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish src/CenitStoryTeller.Web/CenitStoryTeller.Web.csproj -c Release -o /app

# --- runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
# Apply migrations + seed once, then start the webserver. For multi-instance
# deployments, run the --migrate step as a one-shot init container instead.
ENTRYPOINT ["sh", "-c", "dotnet CenitStoryTeller.Web.dll --migrate && exec dotnet CenitStoryTeller.Web.dll"]
