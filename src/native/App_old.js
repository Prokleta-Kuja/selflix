import { StatusBar } from 'expo-status-bar';
import React, { useState } from 'react';
import { Button, StyleSheet, Text, View } from 'react-native';
import { startActivityAsync } from 'expo-intent-launcher';

export default function App() {
  const testFile = process.env['REACT_APP_TEST_FILE']
  const [text, setText] = useState("Tonko Hello world2!");

  const klik = () => setText(testFile || 'NOPE')

  const handlePress = async () => {
    setText("Sviram")

    const activity =
      "android.intent.action.VIEW"
    const result = await startActivityAsync(activity, {
      data: testFile,
      packageName: "org.videolan.vlc",
      className: "org.videolan.vlc.gui.video.VideoPlayerActivity",
      type: "video/*",
      extra: {
        "title": "Test file",
        "from_start": false,
        "position": "4474831",
      }
    })

    setText(result.extra.extra_position + ' / ' + result.extra.extra_duration)
  };

  return (
    <View style={styles.container}>
      <Text style={styles.text}>{text}</Text>
      <StatusBar style="auto" />
      <Button title='test' onPress={klik}>Klikni me</Button>
      <Button title='play' onPress={handlePress}>Sviraj film</Button>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000',
    alignItems: 'center',
    justifyContent: 'center',
  },
  text: {
    color: '#fff'
  }
});
