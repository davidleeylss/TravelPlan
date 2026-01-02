import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import vue3GoogleLogin from 'vue3-google-login'

// 建立 App 實體
const app = createApp(App)

// 設定 Google Client ID
app.use(vue3GoogleLogin, {
    clientId: import.meta.env.VITE_GOOGLE_CLIENT_ID
})

// 掛載到網頁上
app.mount('#app')