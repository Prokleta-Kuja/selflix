FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY ./src/api/out ./

ENV LC_ALL C \
    ASPNETCORE_URLS=http://*:5080 \
    LOCALE=en-US \
    TZ=America/Chicago

ARG DEBIAN_FRONTEND=noninteractive
RUN set -eux; \
    apt-get update && apt-get install -y --no-install-recommends \
    ffmpeg \
    ; \
    rm -rf /var/lib/apt/lists/* \
    ; \
    echo "alias ll='ls -hal'" >> ~/.bashrc \
    ;

VOLUME ["/data/config", "/data/media"]
EXPOSE 5080/TCP
ENTRYPOINT ["dotnet", "selflix.dll"]