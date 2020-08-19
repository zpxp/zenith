FROM mono:5.14.0.177

RUN apt-get update && apt-get install -y wget zip && wget -O docfx.zip https://github.com/dotnet/docfx/releases/download/v2.56.2/docfx.zip && unzip docfx.zip -d docfx
WORKDIR /workspace
VOLUME /workspace

ENTRYPOINT mono /docfx/docfx.exe init -o /workspace/docs