FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG GITHUB_TOKEN
WORKDIR /src
COPY ["Src/BigBang1112.Gbx/Server/BigBang1112.Gbx.Server.csproj", "Src/BigBang1112.Gbx/Server/"]
COPY ["Generators/BigBang1112.Gbx.Server.Generators/BigBang1112.Gbx.Server.Generators.csproj", "Generators/BigBang1112.Gbx.Server.Generators/"]
COPY ["Tools/ReplayViewer/MapViewerEngine/MapViewerEngine.Server/MapViewerEngine.Server.csproj", "Tools/ReplayViewer/MapViewerEngine/MapViewerEngine.Server/"]
COPY ["Tools/ReplayViewer/MapViewerEngine/MapViewerEngine.Shared/MapViewerEngine.Shared.csproj", "Tools/ReplayViewer/MapViewerEngine/MapViewerEngine.Shared/"]
COPY ["Src/BigBang1112.EssentialsV2/Src/BigBang1112.EssentialsV2.Razor/BigBang1112.EssentialsV2.Razor.csproj", "Src/BigBang1112.EssentialsV2/Src/BigBang1112.EssentialsV2.Razor/"]
COPY ["Src/BigBang1112.EssentialsV2/Src/BigBang1112.EssentialsV2/BigBang1112.EssentialsV2.csproj", "Src/BigBang1112.EssentialsV2/Src/BigBang1112.EssentialsV2/"]
COPY ["Src/BigBang1112.Gbx/Client/BigBang1112.Gbx.Client.csproj", "Src/BigBang1112.Gbx/Client/"]
COPY ["Tools/ClipCheckpoint/ClipCheckpoint/ClipCheckpoint.csproj", "Tools/ClipCheckpoint/ClipCheckpoint/"]
COPY ["Tools/ClipInput/ClipInput.Client/ClipInput.Client.csproj", "Tools/ClipInput/ClipInput.Client/"]
COPY ["Tools/ClipInput/ClipInput/ClipInput.csproj", "Tools/ClipInput/ClipInput/"]
COPY ["Tools/ClipToReplay/ClipToReplay/ClipToReplay.csproj", "Tools/ClipToReplay/ClipToReplay/"]
COPY ["Tools/CombineClips/CombineClips/CombineClips.csproj", "Tools/CombineClips/CombineClips/"]
COPY ["Tools/GhostToClip/GhostToClip/GhostToClip.csproj", "Tools/GhostToClip/GhostToClip/"]
COPY ["Tools/ReplayViewer/ReplayViewer/ReplayViewer.csproj", "Tools/ReplayViewer/ReplayViewer/"]
COPY ["Tools/ReplayViewer/MapViewerEngine/MapViewerEngine/MapViewerEngine.csproj", "Tools/ReplayViewer/MapViewerEngine/MapViewerEngine/"]
COPY ["Tools/Spike/Spike.Client/Spike.Client.csproj", "Tools/Spike/Spike.Client/"]
COPY ["Tools/Spike/Spike/Spike.csproj", "Tools/Spike/Spike/"]
COPY ["Tools/EnvimixForTmuf/EnvimixForTmuf/EnvimixForTmuf.csproj", "Tools/EnvimixForTmuf/EnvimixForTmuf/"]
COPY ["Src/BigBang1112.Gbx/Shared/BigBang1112.Gbx.Shared.csproj", "Src/BigBang1112.Gbx/Shared/"]
RUN apk add --no-cache python3
RUN dotnet nuget add source --username USERNAME --password $GITHUB_TOKEN --store-password-in-clear-text --name github "https://nuget.pkg.github.com/bigbang1112-cz/index.json"
RUN dotnet workload install wasm-tools
RUN dotnet restore "Src/BigBang1112.Gbx/Server/BigBang1112.Gbx.Server.csproj"
COPY . .
WORKDIR "/src/Src/BigBang1112.Gbx/Server"
RUN dotnet build "BigBang1112.Gbx.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BigBang1112.Gbx.Server.csproj" -c Release -o /app/publish --self-contained

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./BigBang1112.Gbx.Server"]
