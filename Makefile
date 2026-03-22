ifeq ($(OS),Windows_NT)
	BIN		 := ./MemeIndex.exe
	RUNTIME  := win-x64
	DATA_PUB := C:/Users/$(USERNAME)/AppData/Local
	OPEN     := explorer
else
	BIN		 := ./MemeIndex
	RUNTIME  := linux-x64
	DATA_PUB := $(HOME)/.local/share
	OPEN     := xdg-open
endif

PROJ     := src/MemeIndex/MemeIndex.csproj
WDIR_BIN := out/bin/Release/net8.0
WDIR_PUB := out/bin/Publish/net8.0/$(RUNTIME)/publish

build:
	dotnet build $(PROJ) -c Release --no-restore
build-debug:
	dotnet build $(PROJ) -c Debug   --no-restore
restore:
	dotnet restore $(PROJ)

pub:
	dotnet publish $(PROJ) -r $(RUNTIME) -c Publish --self-contained

run-web:	build
	-$(OPEN) "http://localhost:7373"
	cd $(WDIR_BIN) && $(BIN) --dev -l
run:		build
	cd $(WDIR_BIN) && $(BIN) --dev args.txt
run-test:	build
	cd $(WDIR_BIN) && $(BIN) --dev lab -t
run-demo:	build
	cd $(WDIR_BIN) && $(BIN) --dev lab -D args.txt

clear:
	rm "$(WDIR_BIN)/data/meme-index.db"
	rm "$(WDIR_BIN)/web/thumb" -r
clear-pub:
	rm "$(DATA_PUB)/MemeIndex/meme-index.db"
	rm "$(WDIR_PUB)/web/thumb" -r