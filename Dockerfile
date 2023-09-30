FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY ./src/api/out ./

ENV ASPNETCORE_URLS=http://*:5080 \
    LOCALE=en-US \
    TZ=America/Chicago

VOLUME ["/data/config", "/data/media"]
EXPOSE 5080/TCP
ENTRYPOINT ["dotnet", "selflix.dll"]