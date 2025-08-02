#!/bin/bash



# Ask for sudo password once
sudo -v

# (Optional) If your script might take a while, add this background process to keep sudo alive 
#while true; do sudo -n true; sleep 60; done 2>/dev/null &
#keep_sudo_alive_pid=$!



echo "==============================="
echo "💡 Starting system cleanup..."
echo "==============================="

# Show disk usage before cleanup
echo -e "\n📦 Disk usage before cleanup:"
df -h

# Clear /tmp (temp files)
echo -e "\n🧹 Clearing /tmp..."
sudo rm -rf /tmp/*

# Delete .tst-unix (or .Test-unix) if exists ensure related app is not running
echo -e "\n🧹 Removing .Test-unix if it exists..."
sudo rm -rf /tmp/.Test-unix

#ensure that chromium is not running
if ! pgrep -x "chromium-browser" > /dev/null && ! pgrep -x "chromium" > /dev/null; then
    echo "🧹 Chromium is not running. Safe to delete its temp files."
    sudo rm -rf /tmp/.org.chromium.Chromium.*
else
    echo "⚠️ Chromium is running — skipping deletion of its temp files."
fi


# Clear /var/tmp (persistent temp)
echo -e "\n🧹 Clearing /var/tmp..."
sudo rm -rf /var/tmp/*

# Clear user cache (~/.cache)
echo -e "\n🧹 Clearing user cache (~/.cache)..."
rm -rf ~/.cache/*

# Clear system cache (/var/cache)
echo -e "\n🧹 Clearing system cache (/var/cache)..."
sudo rm -rf /var/cache/*

# Clean apt cache
echo -e "\n🧹 Running apt clean..."
sudo apt clean

# Show disk usage after cleanup
echo -e "\n📦 Disk usage after cleanup:"
df -h

# Kill keep-alive sudo process
#kill "$keep_sudo_alive_pid"

echo "✅ Cleanup completed."

#check if the files really deleted

# ✅ Check if /tmp is empty
echo -e "\n🔍 Checking /tmp..."
if [ -z "$(ls -A /tmp)" ]; then
    echo "✅ /tmp is now empty."
else
    echo "⚠️ /tmp is NOT empty. Contents:"
    ls -A /tmp
fi

# ✅ Check if /.tmp is empty
echo -e "\n🔍 Checking /tmp..."
if [ -z "$(ls -A ~/.tmp)" ]; then
    echo "✅ /.tmp is now empty."
else
    echo "⚠️ /.tmp is NOT empty. Contents:"
    ls -A /tmp
fi

# ✅ Check if /var/tmp is empty
echo -e "\n🔍 Checking /tmp..."
if [ -z "$(ls -A /var/tmp)" ]; then
    echo "✅ /var/tmp is now empty."
else
    echo "⚠️ /var/tmp is NOT empty. Contents:"
    ls -A /tmp
fi

# ✅ Check if /var/cache is empty
echo -e "\n🔍 Checking /tmp..."
if [ -z "$(ls -A /var/cache)" ]; then
    echo "✅ /var/cache is now empty."
else
    echo "⚠️ /var/cache is NOT empty. Contents:"
    ls -A /tmp
fi

mkdir ~/ahmed
cd ~/ahmed

dotnet tool install --global Microsoft.Playwright.CLI

export PATH="$PATH:$HOME/.dotnet/tools"

playwright install

dotnet run

