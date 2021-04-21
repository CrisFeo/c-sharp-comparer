DOTNET='/mnt/c/Program Files/dotnet/dotnet.exe'
SRC_FILES=$(shell find src -name *.cs)

.PHONY: clean
clean:
	$(DOTNET) clean -noLogo -clp:NoSummary | tr -d '\r'

.PHONY: build
build: $(SRC_FILES)
	$(DOTNET) build -noLogo -clp:NoSummary | tr -d '\r'

.PHONY: run
run: $(SRC_FILES)
	$(DOTNET) run | tr -d '\r'

.PHONY: run-release
run-release: $(SRC_FILES)
	$(DOTNET) run -c Release | tr -d '\r'
