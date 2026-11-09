# ---------- Stage 1: the small image the finished website runs on ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# ---------- Stage 2: the big image with the tools needed to build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only the project files first, so Docker can reuse the package download step
COPY ["RaceDay.Mvc/RaceDay.Mvc.csproj", "RaceDay.Mvc/"]
COPY ["RaceDay.Contracts/RaceDay.Contracts.csproj", "RaceDay.Contracts/"]
RUN dotnet restore "RaceDay.Mvc/RaceDay.Mvc.csproj"

# Now copy the source code of the two projects the website needs
COPY RaceDay.Mvc/ RaceDay.Mvc/
COPY RaceDay.Contracts/ RaceDay.Contracts/
WORKDIR "/src/RaceDay.Mvc"
RUN dotnet build "RaceDay.Mvc.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ---------- Stage 3: produce the publishable website ----------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RaceDay.Mvc.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ---------- Stage 4: the final image = small runtime + published website ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]