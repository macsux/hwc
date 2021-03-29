FROM mcr.microsoft.com/dotnet/runtime:3.1 AS runtime3.1
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS sdk5
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8
COPY --from=runtime3.1 ["/Program Files/dotnet/shared", "/Program Files/dotnet/shared"] 
COPY --from=sdk5 ["/Program Files/dotnet", "/Program Files/dotnet"] 

# these are copied from https://github.com/dotnet/dotnet-docker/blob/87cbc30052e5dc892313122e26364b5051df905b/src/sdk/5.0/nanoserver-1809/amd64/Dockerfile#L46-L67
ENV \
    # Unset ASPNETCORE_URLS from aspnet base image
    ASPNETCORE_URLS= \
    #DOTNET_SDK_VERSION=5.0.102 \
    # Enable correct mode for dotnet watch (only mode supported in a container)
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    # Skip extraction of XML docs - generally not useful within an image/container - helps performance
    NUGET_XMLDOC_MODE=skip \
    # PowerShell telemetry for docker image usage
    POWERSHELL_DISTRIBUTION_CHANNEL=PSDocker-DotnetSDK-NanoServer-1809

# In order to set system PATH, ContainerAdministrator must be used
USER ContainerAdministrator
RUN setx /M PATH "%PATH%;C:\Program Files\powershell"
USER ContainerUser
RUN dotnet help