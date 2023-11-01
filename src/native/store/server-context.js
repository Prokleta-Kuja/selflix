import { createContext, useEffect, useState } from "react";
import { OpenAPI, AuthService } from '../api'
import * as OTPAuth from "otpauth";

export const ServerContext = createContext({
    name: '',
    isAuthenticated: false,
    setServer: (srv) => { },
    ensureAuth: () => { },
    clearServer: () => { },
})

const ServerContextProvider = ({ children }) => {
    const [server, setServer] = useState();
    const [expires, setExpires] = useState();

    const connectServer = (srv) => {
        OpenAPI.BASE = srv.origin
        setServer({ ...srv })
    }

    const ensureAuth = () => {
        if (!server) return
        if (expires && Date.now() + 5000 < expires) return

        let totp = new OTPAuth.TOTP({ digits: 6, period: 30, secret: server.secret });
        AuthService.deviceLogin({ requestBody: { deviceId: server.deviceId, totp: totp.generate() } })
            .then(r => {
                if (r.authenticated && r.token && r.expires) {
                    OpenAPI.HEADERS = { authorization: `App ${r.token}` }
                    setExpires(Date.parse(r.expires))
                }
            })
            .catch(e => console.error(e))
    }

    const clearServer = () => {
        setServer(undefined)
        setExpires(undefined)
        AuthService.logout().finally(() => {
            OpenAPI.BASE = undefined
            OpenAPI.HEADERS = undefined
        })
    }

    useEffect(ensureAuth, [server])

    const value = {
        server,
        isAuthenticated: Boolean(expires),
        connectServer,
        ensureAuth,
        clearServer,
    }

    return <ServerContext.Provider value={value}>{children}</ServerContext.Provider>
}

export default ServerContextProvider;