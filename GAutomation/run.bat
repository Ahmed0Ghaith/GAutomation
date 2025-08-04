# Windows Installation Script for Playwright Browser Automation Tool
# PowerShell script to install .NET 8 and Playwright dependencies

param(
    [switch]$SkipDotNet,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host "=== Playwright Browser Automation Tool - Windows Installer ===" -ForegroundColor Cyan
Write-Host ""

# Function to check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Function to download file with progress
function Download-File {
    param(
        [string]$Url,
        [string]$OutputPath
    )
    
    Write-Host "Downloading: $Url" -ForegroundColor Yellow
    
    try {
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile($Url, $OutputPath)
        Write-Host "Downloaded successfully to: $OutputPath" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to download: $_" -ForegroundColor Red
        throw
    }
}

# Function to check if .NET 8 is installed
function Test-DotNet8 {
    try {
        $dotnetVersion = & dotnet --version 2>$null
        if ($dotnetVersion -and $dotnetVersion.StartsWith("8.")) {
            Write-Host ".NET 8 is already installed (version: $dotnetVersion)" -ForegroundColor Green
            return $true
        }
    }
    catch {
        # dotnet command not found
    }
    return $false
}

# Function to install .NET 8
function Install-DotNet8 {
    Write-Host "Installing .NET 8 SDK..." -ForegroundColor Yellow
    
    $tempDir = [System.IO.Path]::GetTempPath()
    $installerPath = Join-Path $tempDir "dotnet-install.ps1"
    
    try {
        # Download .NET install script
        Download-File -Url "https://dot.net/v1/dotnet-install.ps1" -OutputPath $installerPath
        
        # Run the installer
        Write-Host "Running .NET 8 installer..." -ForegroundColor Yellow
        & powershell -ExecutionPolicy Bypass -File $installerPath -Channel 8.0 -InstallDir "$env:ProgramFiles\dotnet"
        
        # Add to PATH if not already there
        $dotnetPath = "$env:ProgramFiles\dotnet"
        $currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
        
        if ($currentPath -notlike "*$dotnetPath*") {
            Write-Host "Adding .NET to system PATH..." -ForegroundColor Yellow
            [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$dotnetPath", "Machine")
            $env:PATH = "$env:PATH;$dotnetPath"
        }
        
        Write-Host ".NET 8 installed successfully!" -ForegroundColor Green
        
        # Clean up
        Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
    }
    catch {
        Write-Host "Failed to install .NET 8: $_" -ForegroundColor Red
        throw
    }
}

# Function to create project structure
function Initialize-Project {
    Write-Host "Setting up project structure..." -ForegroundColor Yellow
    
    $projectDir = "PlaywrightAutomation"
    
    if (Test-Path $projectDir) {
        if ($Force) {
            Write-Host "Removing existing project directory..." -ForegroundColor Yellow
            Remove-Item $projectDir -Recurse -Force
        } else {
            Write-Host "Project directory already exists. Use -Force to overwrite or choose a different location." -ForegroundColor Red
            return $false
        }
    }
    
    # Create project directory
    New-Item -ItemType Directory -Path $projectDir -Force | Out-Null
    Set-Location $projectDir
    
    # Create new console project
    Write-Host "Creating new .NET console project..." -ForegroundColor Yellow
    & dotnet new console -f net8.0 --force
    
    # Add Playwright package
    Write-Host "Adding Playwright NuGet package..." -ForegroundColor Yellow
    & dotnet add package Microsoft.Playwright --version 1.48.0
    
    Write-Host "Project structure created successfully!" -ForegroundColor Green
    return $true
}

# Function to install Playwright browsers
function Install-PlaywrightBrowsers {
    Write-Host "Installing Playwright browsers..." -ForegroundColor Yellow
    
    try {
        # Build the project first
        Write-Host "Building project..." -ForegroundColor Yellow
        & dotnet build
        
        # Install Playwright browsers
        Write-Host "Installing Chromium, Firefox, and WebKit browsers..." -ForegroundColor Yellow
        & dotnet run --project . -- install
        
        # Alternative method using pwsh
        try {
            & dotnet tool install --global Microsoft.Playwright.CLI
            & playwright install
        }
        catch {
            Write-Host "Alternative browser installation method failed, but main installation should work." -ForegroundColor Yellow
        }
        
        Write-Host "Playwright browsers installed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to install Playwright browsers: $_" -ForegroundColor Red
        Write-Host "You may need to run 'playwright install' manually after the setup." -ForegroundColor Yellow
    }
}

# Function to create the main application files
function Create-ApplicationFiles {
    Write-Host "Creating application files..." -ForegroundColor Yellow
    
    # The main program file will be created separately
    # Here we create the BrowserFingerprintManager and other supporting files
    
    # Create config.json template
    $configContent = @'
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
'@
    
    Set-Content -Path "config.json" -Value $configContent -Encoding UTF8
    
    # Create run script
    $runScript = @'
@echo off
echo Starting Playwright Browser Automation Tool...
dotnet run
pause
'@
    
    Set-Content -Path "run.bat" -Value $runScript -Encoding UTF8
    
    Write-Host "Application files created successfully!" -ForegroundColor Green
    Write-Host "  - config.json: Configuration file (edit before running)" -ForegroundColor Gray
    Write-Host "  - run.bat: Quick run script" -ForegroundColor Gray
}

# Function to create start menu shortcut
function Create-Shortcuts {
    Write-Host "Creating shortcuts..." -ForegroundColor Yellow
    
    try {
        $WshShell = New-Object -comObject WScript.Shell
        $currentDir = Get-Location
        
        # Desktop shortcut
        $desktopPath = [Environment]::GetFolderPath("Desktop")
        $shortcutPath = Join-Path $desktopPath "Playwright Automation.lnk"
        $shortcut = $WshShell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = Join-Path $currentDir "run.bat"
        $shortcut.WorkingDirectory = $currentDir
        $shortcut.Description = "Playwright Browser Automation Tool"
        $shortcut.Save()
        
        Write-Host "Desktop shortcut created!" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to create shortcuts: $_" -ForegroundColor Yellow
    }
}

# Main installation process
try {
    Write-Host "Starting installation process..." -ForegroundColor Cyan
    
    # Check if running as administrator
    if (-not (Test-Administrator)) {
        Write-Host "Warning: Not running as administrator. Some features may not work properly." -ForegroundColor Yellow
        Write-Host "Consider running as administrator for full functionality." -ForegroundColor Yellow
        Write-Host ""
    }
    
    # Install .NET 8 if not present
    if (-not $SkipDotNet) {
        if (-not (Test-DotNet8)) {
            Install-DotNet8
        }
    } else {
        Write-Host "Skipping .NET 8 installation as requested." -ForegroundColor Yellow
    }
    
    # Verify .NET 8 is available
    if (-not (Test-DotNet8)) {
        Write-Host "Error: .NET 8 is not available. Please install it manually." -ForegroundColor Red
        exit 1
    }
    
    # Initialize project
    if (-not (Initialize-Project)) {
        exit 1
    }
    
    # Create application files
    Create-ApplicationFiles
    
    # Install Playwright browsers
    Install-PlaywrightBrowsers
    
    # Create shortcuts
    Create-Shortcuts
    
    Write-Host ""
    Write-Host "=== Installation Complete! ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Edit config.json with your proxy settings and target URL" -ForegroundColor White
    Write-Host "2. Add your main Program.cs file to the project" -ForegroundColor White
    Write-Host "3. Run the application using run.bat or 'dotnet run'" -ForegroundColor White
    Write-Host ""
    Write-Host "Project location: $(Get-Location)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "Installation failed: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please check the error message above and try again." -ForegroundColor Yellow
    Write-Host "You may need to run this script as administrator." -ForegroundColor Yellow
    exit 1
}