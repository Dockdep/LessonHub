# .NET-only image. The Angular SPA is now its own project / image
# (see lessonshub-ui/Dockerfile). docker-compose runs them as separate services.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build
WORKDIR /src

COPY LessonsHub/LessonsHub.csproj LessonsHub/
COPY LessonsHub.Application/LessonsHub.Application.csproj LessonsHub.Application/
COPY LessonsHub.Infrastructure/LessonsHub.Infrastructure.csproj LessonsHub.Infrastructure/
COPY LessonsHub.Domain/LessonsHub.Domain.csproj LessonsHub.Domain/
RUN dotnet restore LessonsHub/LessonsHub.csproj

COPY LessonsHub/ LessonsHub/
COPY LessonsHub.Application/ LessonsHub.Application/
COPY LessonsHub.Infrastructure/ LessonsHub.Infrastructure/
COPY LessonsHub.Domain/ LessonsHub.Domain/

RUN dotnet publish LessonsHub/LessonsHub.csproj -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

COPY --from=dotnet-build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "LessonsHub.dll"]
