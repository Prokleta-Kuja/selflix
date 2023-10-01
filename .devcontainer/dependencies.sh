#https://github.com/react-native-community/docker-android/blob/master/Dockerfile

# SDK_VERSION=commandlinetools-linux-9477386_latest.zip
# ANDROID_BUILD_VERSION=34
# ANDROID_TOOLS_VERSION=34.0.0
# NDK_VERSION=25.1.8937393
# CMAKE_VERSION=3.22.1

# ADB_INSTALL_TIMEOUT=10
# ANDROID_HOME=/opt/android
# ANDROID_SDK_ROOT=${ANDROID_HOME}
# ANDROID_NDK_HOME=${ANDROID_HOME}/ndk/$NDK_VERSION

# JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64
# CMAKE_BIN_PATH=${ANDROID_HOME}/cmake/$CMAKE_VERSION/bin

# PATH=${CMAKE_BIN_PATH}:${ANDROID_HOME}/cmdline-tools/latest/bin:${ANDROID_HOME}/emulator:${ANDROID_HOME}/platform-tools:${ANDROID_HOME}/tools:${ANDROID_HOME}/tools/bin:${PATH}

sudo apt update -qq && sudo apt install -qq -y --no-install-recommends \
        ffmpeg \
        apt-transport-https \
        curl \
        file \
        gcc \
        git \
        g++ \
        gnupg2 \
        libgl1 \
        libtcmalloc-minimal4 \
        make \
        openjdk-17-jdk-headless \
        openssh-client \
        patch \
        python3 \
        python3-distutils \
        rsync \
        ruby \
        ruby-dev \
        tzdata \
        unzip \
        sudo \
        ninja-build \
        zip \
        libicu-dev \
        jq \
        shellcheck \
    && sudo gem install bundler \
    && sudo rm -rf /var/lib/apt/lists/*;

sudo curl -sS https://dl.google.com/android/repository/${SDK_VERSION} -o /tmp/sdk.zip \
&& sudo mkdir -p ${ANDROID_HOME}/cmdline-tools \
&& sudo unzip -q -d ${ANDROID_HOME}/cmdline-tools /tmp/sdk.zip \
&& sudo mv ${ANDROID_HOME}/cmdline-tools/cmdline-tools ${ANDROID_HOME}/cmdline-tools/latest \
&& sudo rm /tmp/sdk.zip \
&& yes | sudo ${ANDROID_HOME}/cmdline-tools/latest/bin/sdkmanager --licenses \
&& yes | sudo ${ANDROID_HOME}/cmdline-tools/latest/bin/sdkmanager "platform-tools" \
    "platforms;android-$ANDROID_BUILD_VERSION" \
    "build-tools;$ANDROID_TOOLS_VERSION" \
    "cmake;$CMAKE_VERSION" \
    "ndk;$NDK_VERSION" \
&& sudo rm -rf ${ANDROID_HOME}/.android \
&& sudo chmod 777 -R /opt/android