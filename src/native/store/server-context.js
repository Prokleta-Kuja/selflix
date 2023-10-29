import { createContext, useState } from "react";
import { OpenAPI, AuthService } from '../api'
import * as OTPAuth from "otpauth";

export const ServerContext = createContext({
    token: '',
    name: '',
    deviceId: '',
    origin: '',
    secret: '',
    isAuthenticated: false,
    signin: (token) => { },
    signout: () => { },
    setServer: (srv) => { },
    clearServer: () => { },
})

const ServerContextProvider = ({ children }) => {
    const [token, setToken] = useState();
    const [name, setName] = useState();
    const [deviceId, setDeviceId] = useState();
    const [origin, setOrigin] = useState();
    const [secret, setSecret] = useState();
    const [expires, setExpires] = useState();

    const signin = (token) => {
        setToken(token)
    }

    const signout = () => {
        setToken(null)
    }

    const setServer = (srv) => {
        console.log("Srv", srv)
        setName(srv.name)
        setDeviceId(srv.deviceId)
        setOrigin(srv.origin)
        setSecret(srv.secret)

        let totp = new OTPAuth.TOTP({ digits: 6, period: 30, secret: srv.secret, });
        OpenAPI.BASE = srv.origin
        AuthService.deviceLogin({ requestBody: { deviceId: srv.deviceId, totp: totp.generate() } })
            .then(r => {
                console.log(r)
                setExpires(Date.parse(r.expires))
                const diff = Date.now() - expires
                // TODO: automatic refresh
            })
            .catch(e => console.error(e))
    }

    const clearServer = () => {
        setToken(null)
        setName('')
        setDeviceId('')
        setOrigin('')
        setSecret('')
    }

    const value = {
        token,
        name,
        deviceId,
        origin,
        secret,
        isAuthenticated: !!token,
        signin,
        signout,
        setServer,
        clearServer,
    }

    return <ServerContext.Provider value={value}>{children}</ServerContext.Provider>
}

export default ServerContextProvider;