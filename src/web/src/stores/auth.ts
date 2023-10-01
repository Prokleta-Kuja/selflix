import { defineStore } from 'pinia'
import { ref } from 'vue'
import { AuthService, type AuthStatusModel } from '@/api'
import { useRouter } from 'vue-router'

export const useAuth = defineStore('auth', () => {
  const router = useRouter()
  const initialized = ref(false)
  const isAuthenticated = ref(false)
  const hasOtp = ref(false)
  const username = ref<string | undefined | null>(undefined)

  const setLoginInfo = (info: AuthStatusModel) => {
    isAuthenticated.value = info.authenticated
    hasOtp.value = info.hasOtp
    username.value = info.username
    setExpire(info.expires)
  }

  const clearLoginInfo = () => {
    isAuthenticated.value = false
    hasOtp.value = false
    username.value = ''
  }

  const initialize = () =>
    AuthService.status()
      .then((r) => {
        isAuthenticated.value = r.authenticated
        hasOtp.value = r.hasOtp
        username.value = r.username
        setExpire(r.expires)
      })
      .finally(() => (initialized.value = true))

  const setExpire = (dateTime: string | null | undefined) => {
    if (!dateTime) return

    const dt = new Date(dateTime)
    const time = dt.getTime() - Date.now()
    if (time > 0) setTimeout(onExpire, time)
    else onExpire()
  }

  const onExpire = () => router.push({ name: 'sign-out' })

  return {
    isAuthenticated,
    hasOtp,
    username,
    initialized,
    setLoginInfo,
    clearLoginInfo,
    initialize
  }
})