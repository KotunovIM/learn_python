!apt update -qq
!apt install -y git zip unzip openjdk-17-jdk autoconf libtool \
    pkg-config zlib1g-dev libncurses5-dev libncursesw5-dev \
    libtinfo5 cmake libffi-dev libssl-dev
!pip install -q buildozer cython

from google.colab import files
uploaded = files.upload()          # загрузите checklist_app.zip
!unzip -q checklist_app.zip
%cd checklist_app
!buildozer -v android debug
files.download('bin/checklist-1.0-arm64-v8a-debug.apk')