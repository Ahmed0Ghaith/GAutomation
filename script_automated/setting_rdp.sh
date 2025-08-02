#!/bin/bash



# Ask for sudo password once
sudo -v

#Install prerequisites:

sudo apt update
sudo apt install -y wget apt-transport-https software-properties-common


#Add Microsoft package repository:

wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb



sudo apt update
sudo apt install -y dotnet-sdk-8.0

#Verify installation:

dotnet --version

mkdir -p ~/source/repos
cd ~/source/repos
dotnet new console -n MyPlaywrightTestProject

cd MyPlaywrightTestProject

#Add the Playwright NuGet package:

dotnet add package Microsoft.Playwright
dotnet build

cd ~/GAutomation-kloudytech/
dotnet tool install --global Microsoft.Playwright.CLI
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
source ~/.bashrc                                                                 
echo $PATH
playwright install
dotnet run
