import { useNavigation } from '@react-navigation/native';
import { useEffect, useState, useContext } from 'react';
import { ScrollView, StyleSheet, Text, View, Pressable, BackHandler } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { ServerContext } from '../store/server-context';

// TODO: after replacing server, old server is still used
export default function ServerScreen() {
    const serverCtx = useContext(ServerContext)
    const navigation = useNavigation()
    const [servers, setServers] = useState([])

    const handleNew = () => navigation.navigate('newServer')
    const handleSelect = (srv) => {
        serverCtx.connectServer(srv)
    }

    useEffect(() => {
        const loadServers = async () => {
            try {
                let jsonServers = await AsyncStorage.getItem('servers');
                if (jsonServers === null) jsonServers = "[]"
                setServers(JSON.parse(jsonServers))
            } catch (e) {
            }
        }
        loadServers()
    }, [])

    return <View style={styles.container}>
        <View style={styles.side}></View>
        <ScrollView contentContainerStyle={styles.center}>
            {servers.map((srv) => <Pressable key={srv.name} focusable
                onPress={() => handleSelect(srv)} >
                <View style={styles.item}>
                    <Text style={styles.text}>{srv.name}</Text>
                </View>
            </Pressable>)}
            <Pressable focusable onPress={handleNew} >
                <View style={styles.item}>
                    <Text style={styles.text}>Add new server</Text>
                </View>
            </Pressable>
        </ScrollView>
        <View style={styles.side}></View>
    </View>
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        flexDirection: 'row',
        backgroundColor: '#000',
    },
    side: {
        flex: 1,
    },
    center: {
        flex: 2,
        justifyContent: 'center'
    },
    item: {
        alignItems: 'center',
    },
    text: {
        padding: 25,
        color: '#fff'
    }
});