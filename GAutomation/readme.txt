Windows:
powershell
# Basic installation
.\install-windows.ps1

# Skip .NET if already installed
.\install-windows.ps1 -SkipDotNet

# Force overwrite existing installation
.\install-windows.ps1 -Force
Linux:
bash
# Basic installation
./install-linux.sh

# Skip .NET installation
./install-linux.sh --skip-dotnet

# Custom installation directory
./install-linux.sh --install-dir /path/to/custom/dir
