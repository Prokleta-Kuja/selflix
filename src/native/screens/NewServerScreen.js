import { useState } from 'react';
import { StyleSheet, TextInput, View, Button, Text } from 'react-native';
import { OpenAPI, UserDeviceService } from '../api'
import QRCode from 'react-native-qrcode-svg';
import { useNavigation } from '@react-navigation/native';
import AsyncStorage from '@react-native-async-storage/async-storage';

export default function NewServerScreen() {
  const [url, setUrl] = useState('http://desktop.ica.hr:5080') //TODO: remove
  const [deviceId, setDeviceId] = useState({ id: '', chunked: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const navigation = useNavigation()

  const generateDeviceId = () => {
    setLoading(true)
    let clean = url.toLowerCase().replace(/\/+$/, "")
    if (!clean.startsWith('http'))
      clean = `https://${clean}`
    setUrl(clean)
    OpenAPI.BASE = clean
    UserDeviceService.generateDeviceId()
      .then(r => {
        setDeviceId({ id: r.deviceId, chunked: r.deviceIdChunked })
        setError('')
      })
      .catch(() => setError("Could not connect to server."))
      .finally(() => setLoading(false))
  }

  const registerDevice = () => {
    setLoading(true)
    OpenAPI.BASE = url
    UserDeviceService.registerDevice({ deviceId: deviceId.id })
      .then(async r => {
        setError('')
        try {
          const u = new URL(url)
          let jsonServers = await AsyncStorage.getItem('servers');
          if (jsonServers === null) jsonServers = "[]"
          const servers = JSON.parse(jsonServers)
          const existingIdx = servers.findIndex(srv => srv.name === u.hostname)
          if (existingIdx > -1) {
            console.log("Removing server", servers[existingIdx].name)
            servers.splice(existingIdx, 1)
          }

          servers.push({
            name: u.hostname, // does not contain port or protocol
            origin: u.origin,
            deviceId: deviceId.id,
            secret: r,
          })

          jsonServers = JSON.stringify(servers)
          try {
            await AsyncStorage.setItem('servers', jsonServers);
          } catch (e) {
            console.error(e)
            setError("Failed to set servers")
          }
        } catch (e) {
          console.error(e)
          setError("Failed to read servers")
        }
        if (!error)
          navigation.push('servers')
      })
      .catch(e => setError(e.message))
      .finally(() => setLoading(false))

  }

  if (!deviceId.id)
    return <View style={styles.continer}>
      <View style={styles.centerColumn}>
        <TextInput focusable autoFocus style={styles.srvInput} placeholder='Server address' value={url} onChangeText={setUrl}
          onSubmitEditing={generateDeviceId} />
        {error && <Text style={styles.text}>{error}</Text>}
        <Button title='Add' onPress={generateDeviceId} />
      </View>
    </View>

  return <View style={styles.continer}>
    <View style={styles.centerColumn}>
      <View style={{ marginLeft: 'auto', marginRight: 'auto' }}>
        <QRCode size={300} backgroundColor='#000' color='#fff'
          value={`${url}?deviceId=${deviceId.id}`}
        />
      </View>
      {error && <Text style={styles.text}>{error}</Text>}
      {!error && <Text style={styles.text}>Scan the QR code, or go to {url} and use the following code to add this device. After device has been added, click Continue.</Text>}
      <TextInput style={styles.codeInput} editable={false} value={deviceId.chunked} />
      <Button title='Continue' onPress={registerDevice} />
    </View>
  </View>
}

const styles = StyleSheet.create({
  continer: {
    flex: 1,
    flexDirection: "row",
    backgroundColor: '#000',
    justifyContent: "center",
  },
  centerColumn: {
    width: "50%",
  },
  text: {
    marginVertical: 20,
    color: '#fff',
    fontSize: 25,
  },
  srvInput: {
    backgroundColor: '#252525',
    color: '#fff',
    fontSize: 25,
    textAlign: 'center',
    marginVertical: 15,
  },
  codeInput: {
    backgroundColor: '#252525',
    color: '#fff',
    fontSize: 75,
    textAlign: 'center',
    marginVertical: 15,
  },
})