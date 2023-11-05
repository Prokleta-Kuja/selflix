# Android client

## Debug device

If pairing is needed, open developer tools on device:

- turn on Wireless debugging
- turn on pairing -> should provide pairing code + IP:PORT
- type `adb pair IP:PORT`

Connect to device with `adb connect IP:PORT`, note that port may have changed since pairing

## Generate signing keys

Should be placed in `src/native/android/app/`

```
keytool -genkeypair -v -storetype PKCS12 -keystore my-upload-key.keystore -alias my-key-alias -keyalg RSA -keysize 2048 -validity 10000
```

## On every dev container recreation

Create file `~/.gradle/gradle.properties` (HOME dir!!!) with contents:

```
MYAPP_UPLOAD_STORE_FILE=my-upload-key.keystore
MYAPP_UPLOAD_KEY_ALIAS=my-key-alias
MYAPP_UPLOAD_STORE_PASSWORD=*****
MYAPP_UPLOAD_KEY_PASSWORD=*****
```

## Build a release

```
cd src/native/
# To generate all the Android and IOS files
npx expo prebuild --platform android

# Make sure to connect your device or to run your Emulator
# This generates APK
npx react-native run-android --mode="release"

# If you want to sign the APK and publish to Google Play Store.
# This generates AAB
npx react-native build-android --mode=release
```

Read [documentation](https://reactnative.dev/docs/signed-apk-android)
