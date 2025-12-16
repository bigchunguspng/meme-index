ifeq ($(OS),Windows_NT)
	RUNTIME  := win-x64
	DATA_PUB := C:/Users/$(USERNAME)/AppData/Local
else
	RUNTIME  := linux-x64
	DATA_PUB := $(HOME)/.local/share
endif

PROJ     := src/MemeIndex/MemeIndex.csproj
WDIR_BIN := out/bin/Release/net8.0
WDIR_PUB := out/bin/Publish/net8.0/$(RUNTIME)/publish

pub:
	dotnet publish $(PROJ) -r $(RUNTIME) -c Publish --self-contained

build:
	dotnet build $(PROJ) -c Release

build-debug:
	dotnet build $(PROJ) -c Debug

clear:
	rm "$(WDIR_BIN)/MemeIndex/meme-index.db"
	rm "$(WDIR_BIN)/web/thumb" -r

clear-pub:
	rm "$(DATA_PUB)/MemeIndex/meme-index.db"
	rm "$(WDIR_PUB)/web/thumb" -r