import "react-native-url-polyfill/auto"
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { StatusBar } from 'expo-status-bar';
import React, { useContext } from 'react';
import ServerScreen from './screens/ServerScreen';
import ServerContextProvider, { ServerContext } from './store/server-context';
import NewServerScreen from './screens/NewServerScreen';


const Stack = createNativeStackNavigator();

const defaultStyle = { headerTitleStyle: { color: '#fff' }, headerStyle: { backgroundColor: '#000' } }
export default function App() {
  const serverCtx = useContext(ServerContext)

  if (serverCtx.isAuthenticated)
    return (
      <ServerContextProvider>
        <StatusBar style="dark" />
        <NavigationContainer>
          <Stack.Navigator screenOptions={{ animation: 'slide_from_right' }}>
            {/* <Stack.Screen name='server' component={ServerScreen} /> */}
            <Text>Authenticated</Text>
          </Stack.Navigator>
        </NavigationContainer>
      </ServerContextProvider>
    );

  return <ServerContextProvider>
    <NavigationContainer>
      <Stack.Navigator screenOptions={{ animation: 'slide_from_right' }}>
        <Stack.Screen name='servers' component={ServerScreen} options={{ headerTitle: 'Welcome', ...defaultStyle }} />
        <Stack.Screen name='newServer' component={NewServerScreen} options={{ headerTitle: 'Add server', ...defaultStyle }}>
        </Stack.Screen>
      </Stack.Navigator>
    </NavigationContainer>
  </ServerContextProvider>
}
