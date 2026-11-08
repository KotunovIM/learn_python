# 1. Установить системные зависимости
sudo apt update
sudo apt install -y git zip unzip openjdk-17-jdk python3-pip \
    autoconf libtool pkg-config zlib1g-dev libncurses5-dev \
    libncursesw5-dev libtinfo5 cmake libffi-dev libssl-dev

# 2. Установить buildozer
pip install --user buildozer cython

# 3. Собрать (первый раз ~30-60 минут, качает Android SDK/NDK)
cd checklist_app
buildozer -v android debug

# 4. APK появится в bin/checklist-1.0-arm64-v8a-debug.apk