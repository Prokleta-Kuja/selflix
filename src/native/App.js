import "react-native-url-polyfill/auto"
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import React, { useContext } from 'react';
import ServerScreen from './screens/ServerScreen';
import ServerContextProvider, { ServerContext } from './store/server-context';
import NewServerScreen from './screens/NewServerScreen';
import LibraryScreen from "./screens/LibraryScreen";
import DirScreen from "./screens/DirScreen";

const Stack = createNativeStackNavigator();

const defaultStyle = { headerTitleStyle: { color: '#fff' }, headerStyle: { backgroundColor: '#000' } }
export default function App() {
  return <ServerContextProvider>
    <Navigation />
  </ServerContextProvider>
}

function Navigation() {
  const serverCtx = useContext(ServerContext)

  return <NavigationContainer>
    <Stack.Navigator screenOptions={{ animation: 'slide_from_right' }}>
      {serverCtx.isAuthenticated ? (<>
        <Stack.Screen name='libraries' component={LibraryScreen} options={{ headerTitle: 'Libraries', ...defaultStyle }} />
        <Stack.Screen name='dir' component={DirScreen} options={{ headerTitle: 'Dir', ...defaultStyle }} />
      </>) : (<>
        <Stack.Screen name='servers' component={ServerScreen} options={{ animation: 'slide_from_left', headerTitle: 'Welcome', ...defaultStyle }} />
        <Stack.Screen name='newServer' component={NewServerScreen} options={{ headerTitle: 'Add server', ...defaultStyle }} />
      </>)}
    </Stack.Navigator>
  </NavigationContainer>
}