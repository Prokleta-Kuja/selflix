import { useEffect, useState, useContext } from 'react';
import { useNavigation, useRoute } from '@react-navigation/native';
import { ScrollView, StyleSheet, Text, View, Pressable, BackHandler } from 'react-native';
import { ServerContext } from '../store/server-context';
import { OpenAPI, LibraryService, StreamService } from '../api';
import { startActivityAsync } from 'expo-intent-launcher';

export default function DirScreen() {
    const navigation = useNavigation()
    const route = useRoute().params
    const serverCtx = useContext(ServerContext)
    const [dirs, setDirs] = useState([])
    const [videos, setVideos] = useState([])

    useEffect(() => {
        const loadDir = async () => {
            serverCtx.ensureAuth()
            LibraryService.getDirectory({ libraryId: route.libId, directoryId: route.dirId })
                .then(r => {
                    setDirs(r.subDirs ?? [])
                    setVideos(r.videos ?? [])
                    console.log("kita", r)
                })
                .catch(e => console.error(e))
        }
        loadDir()

        const backAction = () => {
            navigation.goBack()
            return true;
        };

        const backHandler = BackHandler.addEventListener(
            'hardwareBackPress',
            backAction,
        );

        return () => backHandler.remove();
    }, [])

    const handleSelect = (dir) => {
        console.log("Selected", dir)
        navigation.push('dir', { libId: route.libId, dirId: dir.id })
    }

    const handlePlay = async (vid) => {
        const key = await StreamService.requestStreamKey({ videoId: vid.id })
        const url = `${OpenAPI.BASE}/api/stream/${key}`

        const activity =
            "android.intent.action.VIEW"
        const result = await startActivityAsync(activity, {
            data: url,
            packageName: "org.videolan.vlc",
            className: "org.videolan.vlc.gui.video.VideoPlayerActivity",
            type: "video/*",
            extra: {
                "title": vid.title,
                "from_start": true,
                //"position": "4474831",
            }
        })
        await StreamService.completeStreamKey({ streamKey: key })
        console.log("Playback ended", result.extra)
    }

    return <View style={styles.container}>
        <View style={styles.side}></View>
        <ScrollView contentContainerStyle={styles.center}>
            {dirs.map((dir) => <Pressable key={dir.id} focusable
                onPress={() => handleSelect(dir)} >
                <View style={styles.item}>
                    <Text style={styles.text}>{dir.name}</Text>
                </View>
            </Pressable>)}
            {videos.map((vid) => <Pressable key={vid.id} focusable
                onPress={() => handlePlay(vid)} >
                <View style={styles.item}>
                    <Text style={styles.text}>{vid.title}</Text>
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