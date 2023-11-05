import { useEffect, useState, useContext } from 'react';
import { useNavigation } from '@react-navigation/native';
import { ScrollView, StyleSheet, Text, View, Pressable, BackHandler } from 'react-native';
import { ServerContext } from '../store/server-context';
import { LibraryService } from '../api';

export default function LibraryScreen() {
    const navigation = useNavigation()
    const serverCtx = useContext(ServerContext)
    const [libraries, setLibraries] = useState([])

    useEffect(() => {
        const loadLibraries = async () => {
            serverCtx.ensureAuth()
            LibraryService.getAllLibraries({ size: 100 })
                .then(r => setLibraries(r.items))
                .catch(e => console.error(e))
        }
        loadLibraries()

        const backAction = () => {
            serverCtx.clearServer()
            return true;
        };

        const backHandler = BackHandler.addEventListener(
            'hardwareBackPress',
            backAction,
        );

        return () => backHandler.remove();
    }, [])

    const handleSelect = (lib) => {
        navigation.push('dir', { libId: lib.id })
    }

    return <View style={styles.container}>
        <View style={styles.side}></View>
        <ScrollView contentContainerStyle={styles.center}>
            {libraries.map((lib) => <Pressable key={lib.name} focusable
                onPress={() => handleSelect(lib)} >
                <View style={styles.item}>
                    <Text style={styles.text}>{lib.name}</Text>
                </View>
            </Pressable>)}
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