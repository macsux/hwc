FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR src
# copy nuget.config files at solution and project levels
# copy any local nuget sources that are subfolders of the solution
COPY ["build/_build.csproj", "build/_build.csproj"]
RUN dotnet restore "build\_build.csproj"
COPY . .
RUN dotnet msbuild /p:RestorePackages=false /t:PublishLayer /p:PublishDir=/layer/ /p:DockerLayer=All "build/_build.csproj"
FROM mcr.microsoft.com/dotnet/runtime:5.0 AS run
WORKDIR /app
COPY --from=build /layer/package ./
COPY --from=build /layer/earlypackage ./
COPY --from=build /layer/project ./
COPY --from=build /layer/app ./
ENTRYPOINT ["dotnet", "_build.dll"]
