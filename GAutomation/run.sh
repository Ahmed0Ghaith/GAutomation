#!/bin/bash

# Linux Installation Script for Playwright Browser Automation Tool
# Bash script to install .NET 8 and Playwright dependencies

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Default values
SKIP_DOTNET=false
FORCE=false
INSTALL_DIR="$HOME/PlaywrightAutomation"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-dotnet)
            SKIP_DOTNET=true
            shift
            ;;
        --force)
            FORCE=true
            shift
            ;;
        --install-dir)
            INSTALL_DIR="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  --skip-dotnet     Skip .NET 8 installation"
            echo "  --force          Force overwrite existing installation"
            echo "  --install-dir    Installation directory (default: $HOME/PlaywrightAutomation)"
            echo "  -h, --help       Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo -e "${CYAN}=== Playwright Browser Automation Tool - Linux Installer ===${NC}"
echo ""

# Function to detect Linux distribution
detect_distro() {
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        DISTRO=$ID
        VERSION=$VERSION_ID
    elif [ -f /etc/redhat-release ]; then
        DISTRO="centos"
    elif [ -f /etc/debian_version ]; then
        DISTRO="debian"
    else
        DISTRO="unknown"
    fi
    
    echo -e "${GRAY}Detected distribution: $DISTRO${NC}"
}

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to check if .NET 8 is installed
check_dotnet8() {
    if command_exists dotnet; then
        local version=$(dotnet --version 2>/dev/null)
        if [[ $version == 8.* ]]; then
            echo -e "${GREEN}.NET 8 is already installed (version: $version)${NC}"
            return 0
        fi
    fi
    return 1
}

# Function to install system dependencies
install_system_dependencies() {
    echo -e "${YELLOW}Installing system dependencies...${NC}"
    
    case $DISTRO in
        ubuntu|debian)
            sudo apt-get update
            sudo apt-get install -y wget curl apt-transport-https software-properties-common
            
            # Install dependencies for Playwright browsers
            sudo apt-get install -y \
                libnss3-dev \
                libatk-bridge2.0-dev \
                libdrm-dev \
                libxkbcommon-dev \
                libgtk-3-dev \
                libgbm-dev \
                libasound2-dev \
                libxss1 \
                libgconf-2-4
            ;;
        fedora|centos|rhel)
            if command_exists dnf; then
                sudo dnf install -y wget curl
                sudo dnf install -y \
                    nss-devel \
                    atk-devel \
                    libdrm-devel \
                    libxkbcommon-devel \
                    gtk3-devel \
                    mesa-libgbm-devel \
                    alsa-lib-devel
            else
                sudo yum install -y wget curl
                sudo yum install -y \
                    nss-devel \
                    atk-devel \
                    libdrm-devel \
                    libxkbcommon-devel \
                    gtk3-devel \
                    mesa-libgbm-devel \
                    alsa-lib-devel
            fi
            ;;
        arch)
            sudo pacman -Sy --noconfirm wget curl
            sudo pacman -Sy --noconfirm \
                nss \
                atk \
                libdrm \
                libxkbcommon \
                gtk3 \
                mesa \
                alsa-lib
            ;;
        *)
            echo -e "${YELLOW}Unknown distribution. You may need to install dependencies manually.${NC}"
            ;;
    esac
    
    echo -e "${GREEN}System dependencies installed successfully!${NC}"
}

# Function to install .NET 8
install_dotnet8() {
    echo -e "${YELLOW}Installing .NET 8 SDK...${NC}"
    
    # Download and install Microsoft package signing key
    case $DISTRO in
        ubuntu|debian)
            wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
            
            sudo apt-get update
            sudo apt-get install -y dotnet-sdk-8.0
            ;;
        fedora)
            sudo dnf install -y https://packages.microsoft.com/config/fedora/$(rpm -E %fedora)/packages-microsoft-prod.rpm
            sudo dnf install -y dotnet-sdk-8.0
            ;;
        centos|rhel)
            sudo yum install -y https://packages.microsoft.com/config/centos/$(rpm -E %centos)/packages-microsoft-prod.rpm
            sudo yum install -y dotnet-sdk-8.0
            ;;
        *)
            # Use the universal install script
            echo -e "${YELLOW}Using universal .NET install script...${NC}"
            curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
            
            # Add to PATH
            export DOTNET_ROOT=$HOME/.dotnet
            export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
            
            # Add to shell profile
            if [ -f "$HOME/.bashrc" ]; then
                echo 'export DOTNET_ROOT=$HOME/.dotnet' >> "$HOME/.bashrc"
                echo 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >> "$HOME/.bashrc"
            fi
            
            if [ -f "$HOME/.zshrc" ]; then
                echo 'export DOTNET_ROOT=$HOME/.dotnet' >> "$HOME/.zshrc"
                echo 'export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >> "$HOME/.zshrc"
            fi
            ;;
    esac
    
    echo -e "${GREEN}.NET 8 installed successfully!${NC}"
}

# Function to create project structure
initialize_project() {
    echo -e "${YELLOW}Setting up project structure...${NC}"
    
    if [ -d "$INSTALL_DIR" ]; then
        if [ "$FORCE" = true ]; then
            echo -e "${YELLOW}Removing existing project directory...${NC}"
            rm -rf "$INSTALL_DIR"
        else
            echo -e "${RED}Project directory already exists. Use --force to overwrite.${NC}"
            return 1
        fi
    fi
    
    # Create project directory
    mkdir -p "$INSTALL_DIR"
    cd "$INSTALL_DIR"
    
    # Create new console project
    echo -e "${YELLOW}Creating new .NET console project...${NC}"
    dotnet new console -f net8.0 --force
    
    # Add Playwright package
    echo -e "${YELLOW}Adding Playwright NuGet package...${NC}"
    dotnet add package Microsoft.Playwright --version 1.48.0
    
    echo -e "${GREEN}Project structure created successfully!${NC}"
    return 0
}

# Function to install Playwright browsers
install_playwright_browsers() {
    echo -e "${YELLOW}Installing Playwright browsers...${NC}"
    
    # Build the project first
    echo -e "${YELLOW}Building project...${NC}"
    dotnet build
    
    # Install Playwright CLI tool
    echo -e "${YELLOW}Installing Playwright CLI tool...${NC}"
    dotnet tool install --global Microsoft.Playwright.CLI || true
    
    # Add .NET tools to PATH if not already there
    export PATH="$PATH:$HOME/.dotnet/tools"
    
    # Install browsers
    echo -e "${YELLOW}Installing Chromium, Firefox, and WebKit browsers...${NC}"
    playwright install || {
        echo -e "${YELLOW}Direct playwright install failed, trying alternative method...${NC}"
        dotnet exec playwright install || {
            echo -e "${YELLOW}Browser installation failed. You may need to run 'playwright install' manually.${NC}"
        }
    }
    
    # Install system dependencies for browsers
    echo -e "${YELLOW}Installing browser system dependencies...${NC}"
    playwright install-deps || true
    
    echo -e "${GREEN}Playwright browsers installed successfully!${NC}"
}

# Function to create application files
create_application_files() {
    echo -e "${YELLOW}Creating application files...${NC}"
    
    # Create config.json template
    cat > config.json << 'EOF'
{
  "proxy": {
    "server": "https://geo.iproyal.com:12321",
    "username": "your_proxy_username",
    "password": "your_proxy_password"
  },
  "targetUrl": "https://www.google.com",
  "noOfInstances": 1,
  "loopCount": 1,
  "actionSequence": [
    {
      "type": "wait",
      "duration": 5,
      "description": "Wait for 5 seconds"
    },
    {
      "type": "scroll_down",
      "duration": 20,
      "description": "Scroll down for 20 seconds"
    },
    {
      "type": "scroll_up",
      "duration": 20,
      "description": "Scroll up for 20 seconds"
    },
    {
      "type": "click",
      "selector": "#example-button",
      "description": "Click on example button"
    }
  ]
}
EOF
    
    # Create run script
    cat > run.sh << 'EOF'
#!/bin/bash
echo "Starting Playwright Browser Automation Tool..."

# Ensure .NET tools are in PATH
export PATH="$PATH:$HOME/.dotnet/tools"

# Set display for GUI applications (if running in WSL or similar)
if [ -z "$DISPLAY" ]; then
    export DISPLAY=:0
fi

dotnet run
EOF
    
    chmod +x run.sh
    
    # Create desktop entry for GUI environments
    if [ -d "$HOME/.local/share/applications" ]; then
        cat > "$HOME/.local/share/applications/playwright-automation.desktop" << EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=Playwright Browser Automation Tool
Comment=Automated browser testing and interaction tool
Exec=$INSTALL_DIR/run.sh
Icon=applications-internet
Path=$INSTALL_DIR
Terminal=true
Categories=Development;Network;
EOF
        
        echo -e "${GREEN}Desktop entry created!${NC}"
    fi
    
    echo -e "${GREEN}Application files created successfully!${NC}"
    echo -e "${GRAY}  - config.json: Configuration file (edit before running)${NC}"
    echo -e "${GRAY}  - run.sh: Quick run script${NC}"
}

# Function to create uninstall script
create_uninstall_script() {
    cat > uninstall.sh << 'EOF'
#!/bin/bash

echo "Uninstalling Playwright Browser Automation Tool..."

# Remove desktop entry
rm -f "$HOME/.local/share/applications/playwright-automation.desktop"

# Remove .NET tools (optional)
read -p "Remove .NET global tools? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    dotnet tool uninstall --global Microsoft.Playwright.CLI
fi

# Remove project directory
read -p "Remove entire project directory? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    cd ..
    rm -rf "$(dirname "$0")"
    echo "Project directory removed."
else
    echo "Project directory kept."
fi

echo "Uninstallation complete."
EOF
    
    chmod +x uninstall.sh
    echo -e "${GREEN}Uninstall script created: uninstall.sh${NC}"
}

# Main installation process
main() {
    echo -e "${CYAN}Starting installation process...${NC}"
    
    # Detect distribution
    detect_distro
    
    # Install system dependencies
    install_system_dependencies
    
    # Install .NET 8 if not present and not skipped
    if [ "$SKIP_DOTNET" = false ]; then
        if ! check_dotnet8; then
            install_dotnet8
        fi
    else
        echo -e "${YELLOW}Skipping .NET 8 installation as requested.${NC}"
    fi
    
    # Verify .NET 8 is available
    if ! check_dotnet8; then
        echo -e "${RED}Error: .NET 8 is not available. Please install it manually.${NC}"
        exit 1
    fi
    
    # Initialize project
    if ! initialize_project; then
        exit 1
    fi
    
    # Create application files
    create_application_files
    
    # Install Playwright browsers
    install_playwright_browsers
    
    # Create uninstall script
    create_uninstall_script
    
    echo ""
    echo -e "${GREEN}=== Installation Complete! ===${NC}"
    echo ""
    echo -e "${CYAN}Next steps:${NC}"
    echo -e "${GRAY}1. Edit config.json with your proxy settings and target URL${NC}"
    echo -e "${GRAY}2. Add your main Program.cs file to the project${NC}"
    echo -e "${GRAY}3. Run the application using ./run.sh or 'dotnet run'${NC}"
    echo ""
    echo -e "${GRAY}Project location: $INSTALL_DIR${NC}"
    echo -e "${GRAY}To uninstall: ./uninstall.sh${NC}"
    echo ""
}

# Check if script is run with sudo (not recommended)
if [ "$EUID" -eq 0 ]; then
    echo -e "${YELLOW}Warning: Running as root is not recommended.${NC}"
    echo -e "${YELLOW}Consider running as a regular user.${NC}"
    read -p "Continue anyway? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Run main installation
main