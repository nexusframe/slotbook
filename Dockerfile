# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore reads only the files that describe the dependency graph, so those are copied alone and
# first. Editing Program.cs then leaves this layer - and the NuGet download inside it - valid.
# Copying the sources first instead produces a correct image that re-downloads every package on
# every source change.
#
# .editorconfig belongs in this set: EnforceCodeStyleInBuild plus TreatWarningsAsErrors make the
# formatting rules part of compilation, so without the file the container would build against
# different defaults than the machine the code was written on.
COPY global.json Directory.Build.props Directory.Packages.props dotnet-tools.json .editorconfig ./
COPY src/SlotBook.Api/SlotBook.Api.csproj src/SlotBook.Api/
COPY src/SlotBook.Core/SlotBook.Core.csproj src/SlotBook.Core/
COPY src/SlotBook.Infrastructure/SlotBook.Infrastructure.csproj src/SlotBook.Infrastructure/

RUN dotnet restore src/SlotBook.Api/SlotBook.Api.csproj
RUN dotnet tool restore

COPY src/ src/

RUN dotnet publish src/SlotBook.Api/SlotBook.Api.csproj \
    --configuration Release --no-restore --output /app

# A migration bundle: one executable carrying every migration in the assembly, run once by the
# migrator service before the API starts. The alternative - Database.Migrate() in Program.cs -
# hands schema changes to the instance that is about to serve traffic, and turns startup into a
# race as soon as there is more than one of them.
#
# The connection string here is a placeholder. Building the bundle contacts no database, but EF
# reaches the DbContext registration by executing Program.cs, which throws when the string is
# missing. The real one arrives as an environment variable when the bundle runs.
RUN ConnectionStrings__SlotBook="Server=none;Database=SlotBook" \
    dotnet ef migrations bundle \
        --project src/SlotBook.Infrastructure \
        --startup-project src/SlotBook.Api \
        --configuration Release --force --output /migrator/migrate

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app ./
COPY --from=build /migrator /migrator

# The base image defines APP_UID but still starts as root; this line is what drops the privilege.
# It is also the reason the port is 8080 rather than 80: a non-root process cannot bind below
# 1024, which is why the .NET container images changed their default in .NET 8.
USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "SlotBook.Api.dll"]
