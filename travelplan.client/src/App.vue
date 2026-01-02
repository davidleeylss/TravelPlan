<script setup>
    import { ref, computed, onMounted, watch, reactive } from 'vue';
    import axios from 'axios';
    import { googleTokenLogin } from 'vue3-google-login'; // 引入 Google 方法

    

    // 攔截器設定：每次發送 API 前，自動把 Token 帶在 Header
    axios.interceptors.request.use(config => {
        const token = localStorage.getItem('jwt_token');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    });

    // --- 旅遊相關變數 ---
    const currentTripId = ref(null); // 目前選中的旅遊 ID
    const myTrips = ref([]); // 我的所有旅遊列表
    const showCreateTripModal = ref(false); // 顯示新增旅遊視窗
    const newTripForm = ref({ title: '', startDate: '', endDate: '', participants: [] });

    // --- 登入相關變數 ---
    const isLoggedIn = ref(false); // 是否已登入
    const isRegisterMode = ref(false); // 切換登入/註冊模式
    const authForm = ref({ username: '', password: '' }); // 表單資料

    // 目前的使用者
    const currentUser = ref('');
    const currentUserId = ref(0);
    // 旅程的所有成員
    const allMembers = ref([]);

    // --- 登入/註冊功能 ---
    const handleAuth = async () => {
        if (!authForm.value.username || !authForm.value.password) {
            alert("請輸入帳號密碼");
            return;
        }

        try {
            if (isRegisterMode.value) {
                // 註冊
                await axios.post('/api/auth/register', authForm.value);
                alert("註冊成功！請登入");
                isRegisterMode.value = false; // 切換回登入畫面
            } else {
                // 登入
                const res = await axios.post('/api/auth/login', authForm.value);
                loginSuccess(res.data);
            }
        } catch (err) {
            alert(err.response?.data || "發生錯誤");
        }
    };

    // 處理 Google 登入
    const handleGoogleLogin = () => {
        googleTokenLogin().then(async (response) => {
            try {
                const res = await axios.post('/api/auth/google-login', {
                    accessToken: response.access_token
                });
                loginSuccess(res.data);
            } catch (err) {
                alert("Google 登入失敗");
            }
        });
    };

    // 登入成功共用邏輯
    const loginSuccess = (data) => {
        // 把 Token 存起來
        localStorage.setItem('jwt_token', data.token);
        localStorage.setItem('user_name', data.username);
        localStorage.setItem('user_id', data.id);

        // 更新狀態
        currentUser.value = data.username;
        currentUserId.value = data.id;
        isLoggedIn.value = true;

        // 抓資料
        fetchMyTrips(data.id);
        fetchAllMembers();
    };

    

    //登出
    const logout = () => {
        localStorage.removeItem('jwt_token'); // 清除 Token
        localStorage.removeItem('user_name'); // 清除 user_name
        localStorage.removeItem('user_id');   // 清除 user_id

        isLoggedIn.value = false;
        currentUser.value = '';
        currentUserId.value = 0;
        authForm.value = { username: '', password: '' };
    };
    // 檢查是否已登入 (重新整理頁面時保持登入)
    onMounted(() => {
        const token = localStorage.getItem('jwt_token');
        if (token) {
            isLoggedIn.value = true;
            // 從 LocalStorage 撈回使用者資訊
            const savedName = localStorage.getItem('user_name');
            const savedId = localStorage.getItem('user_id');
            if (savedName) currentUser.value = savedName;
            if (savedId) {
                currentUserId.value = parseInt(savedId);
                // 抓登入人員的旅遊列表
                fetchMyTrips(currentUserId.value);
                fetchAllMembers();
            }
        }
    });

    // 取得所有註冊過的使用者 (用來當作參加者名單)
    const fetchAllMembers = async () => {
        try {
            const res = await axios.get('/api/auth/users');
            allMembers.value = res.data;
            selectedParticipants.value = [...allMembers.value]; // 預設全選
        } catch (err) {
            console.error(err);
        }
    };
    // 用來判斷現在 Trip Modal 是「建立模式」還是「編輯模式」
    const isEditingTrip = ref(false);
    const editingTripId = ref(0);

    // --- 開啟建立視窗 ---
    const openCreateTripModal = () => {
        isEditingTrip.value = false; // 設定為建立模式
        newTripForm.value = { title: '', startDate: '', endDate: '', participants: [] }; // 清空表單
        // 預設把自己加入參加者 (顯示在 UI 上比較直覺)
        if (currentUser.value && !newTripForm.value.participants.includes(currentUser.value)) {
            newTripForm.value.participants.push(currentUser.value);
        }
        showCreateTripModal.value = true;
    };

    // --- 開啟編輯視窗 (點擊筆的圖示觸發) ---
    const openEditTripModal = (trip, event) => {
        event.stopPropagation(); // 阻止冒泡 (避免點編輯時誤觸進入旅遊)

        isEditingTrip.value = true; // 設定為編輯模式
        editingTripId.value = trip.id;

        // 把舊資料填入表單
        newTripForm.value = {
            title: trip.title,
            startDate: trip.startDate.split('T')[0], // 格式化日期
            endDate: trip.endDate.split('T')[0],
            // 把物件陣列轉成純文字陣列 (User object -> Username string)
            participants: trip.travelGroup?.members.map(m => m.username) || []
        };

        showCreateTripModal.value = true;
    };

    // --- 提交表單 (判斷是新增還是更新) ---
    const submitTripForm = async () => {
        try {
            const payload = {
                ...newTripForm.value,
                ownerId: currentUserId.value,
            };

            if (isEditingTrip.value) {
                // --- 編輯模式 ---
                await axios.put(`/api/trip/${editingTripId.value}`, payload);
                alert("更新成功！");
            } else {
                // --- 建立模式 ---
                await axios.post('/api/trip', payload);
                alert("建立成功！");
            }

            showCreateTripModal.value = false;
            fetchMyTrips(currentUserId.value); // 重整列表

        } catch (err) {
            console.error(err);
            alert("操作失敗: " + (err.response?.data || err.message));
        }
    };

    // --- 抓取旅遊列表 ---
    const fetchMyTrips = async (userId) => {
        try {
            const res = await axios.get(`/api/trip?userId=${userId}`);
            myTrips.value = res.data;
        } catch (err) {
            console.error(err);
        }
    };

    // --- 刪除旅遊 (或退出) ---
    const deleteTrip = async (trip, event) => {
        event.stopPropagation(); // 阻止冒泡

        const isOwner = trip.ownerId === currentUserId.value;
        const msg = isOwner
            ? `確定要刪除「${trip.title}」嗎？所有資料都會消失喔！`
            : `確定要退出「${trip.title}」嗎？`;

        if (!confirm(msg)) return;

        try {
            // 傳入 userId 讓後端判斷是刪除還是退出
            await axios.delete(`/api/trip/${trip.id}?userId=${currentUserId.value}`);

            alert(isOwner ? "已刪除" : "已退出");
            fetchMyTrips(currentUserId.value); // 重整列表
        } catch (err) {
            console.error(err);
            alert("刪除失敗");
        }
    };

    // --- 進入旅遊 ---
    const selectTrip = (trip) => {
        currentTripId.value = trip.id;
        // 設定日期區間給原本的 UI 用
        dates.value = getDatesInRange(trip.startDate, trip.endDate);
        currentDate.value = dates.value[0];

        // 載入該旅遊的詳細資料 (記得改這些函式，讓它們帶 tripId 去後端)
        fetchItineraries(trip.id);
        fetchFlights(trip.id);
        fetchExpenses(trip.id);
    };

    // --- 離開旅遊 (回到列表) ---
    const backToTrips = () => {
        currentTripId.value = null;
        itineraries.value = [];

        // 重置機票資料 (手動清空欄位)
        flightInfo.outbound = {
            id: 0, type: 'Outbound', participants: '', date: '',
            departureTime: '', arrivalTime: '', departure: '', arrival: '', airline: '', number: ''
        };
        flightInfo.inbound = {
            id: 0, type: 'Inbound', participants: '', date: '',
            departureTime: '', arrivalTime: '', departure: '', arrival: '', airline: '', number: ''
        };
    };

    // 計算日期區間
    const getDatesInRange = (startDate, endDate) => {
        const date = new Date(startDate);
        const end = new Date(endDate);
        const dates = [];
        while (date <= end) {
            dates.push(new Date(date).toISOString().split('T')[0]);
            date.setDate(date.getDate() + 1);
        }
        return dates;
    };
    
    // 目前正在編輯的行程，有哪些參加者 (預設全選)
    const selectedParticipants = ref([...allMembers.value]);

    // 機票資料 (改回預設空值，等待 API 填入)
    const flightInfo = reactive({
        outbound: [], // 去程清單
        inbound: []   // 回程清單
    });

    // 產生一張空白機票
    const createEmptyFlight = (type) => ({
        id: 0,
        type: type, // 'Outbound' 或 'Inbound'
        tripId: currentTripId.value,
        participants: currentUser.value, // 預設參加者是自己
        date: '',
        departureTime: '',
        arrivalTime: '',
        departure: '',
        arrival: '',
        airline: '',
        number: ''
    });

    // API: 讀取機票
    const fetchFlights = async (tripId) => {
        if (!tripId) return;
        try {
            const res = await axios.get(`/api/flight?tripId=${tripId}`);
            const data = res.data; // 這是陣列

            // 過濾並排序 (依照時間早晚排序，轉機順序才對)
            const sortByTime = (a, b) => (a.date + a.departureTime).localeCompare(b.date + b.departureTime);

            flightInfo.outbound = data.filter(f => f.type === 'Outbound').sort(sortByTime);
            flightInfo.inbound = data.filter(f => f.type === 'Inbound').sort(sortByTime);

            // 如果完全沒資料，預設各給一張空白的
            if (flightInfo.outbound.length === 0) addSegment('Outbound');
            if (flightInfo.inbound.length === 0) addSegment('Inbound');

        } catch (err) {
            console.error("讀取機票失敗", err);
        }
    };

    // 加入一段轉機
    const addSegment = (type) => {
        if (type === 'Outbound') flightInfo.outbound.push(createEmptyFlight('Outbound'));
        else flightInfo.inbound.push(createEmptyFlight('Inbound'));
    };
    // 移除一段機票
    const removeSegment = async (flight, list, index) => {
        if (!confirm("確定要移除這段航班嗎?")) return;

        // 如果這張票已經存在資料庫 (有 ID)，要呼叫 API 刪除
        if (flight.id !== 0) {
            try {
                await axios.delete(`/api/flight/${flight.id}`);
            } catch (err) {
                alert("刪除失敗");
                return;
            }
        }
        // 從前端陣列移除
        list.splice(index, 1);
    };


    // 重置機票欄位，避免切換旅遊時資料混亂
    const resetFlightInfo = () => {
        const defaultFlight = {
            id: 0, participants: '', date: '', departureTime: '', arrivalTime: '',
            departure: '', arrival: '', airline: '', number: ''
        };
        flightInfo.outbound = { ...defaultFlight, type: 'Outbound' };
        flightInfo.inbound = { ...defaultFlight, type: 'Inbound' };
    };

    // API: 儲存機票 (修改 toggleEdit 函式)
    const toggleEdit = async () => {
        if (isEditingFlight.value) {
            try {
                // 把所有去程跟回程合併成一個大陣列，一次跑迴圈儲存
                const allFlights = [...flightInfo.outbound, ...flightInfo.inbound];

                for (const flight of allFlights) {
                    // 只要有填寫其中一項重要資訊 (包含航班編號)，就視為有效機票
                    if (flight.airline || flight.departure || flight.arrival || flight.number) {

                        await axios.post('/api/flight', {
                            ...flight, 
                            tripId: currentTripId.value, 
                            participants: flight.participants || currentUser.value
                        });
                    }
                }

                alert('機票資訊已儲存！');
                fetchFlights(currentTripId.value); // 重抓以更新 ID
            } catch (err) {
                console.error("儲存失敗", err);
                alert("儲存失敗");
            }
        }
        isEditingFlight.value = !isEditingFlight.value;
    };

    // 監聽使用者切換
    watch(currentUser, () => {
        // 當使用者改變時，重新讀取該使用者的機票與行程
        fetchFlights();
        fetchItineraries(); // 注意：你的行程 API 也要記得改成能吃 owner 參數
    });

    const currentTab = ref('itinerary'); // 記錄現在是用哪個分頁
    const currentDate = ref('2026-02-21');
    const dates = ['2026-02-21', '2026-02-22', '2026-02-23', '2026-02-24', '2026-02-25', '2026-02-26', '2026-02-27', '2026-02-28'];
    const showAddModal = ref(false); // 記錄彈出視窗要不要顯示
    

    // 控制是否正在編輯機票
    const isEditingFlight = ref(false);

    // 判斷目前是否為編輯模式
    const isEditing = ref(false);

    // --- 地圖相關變數 ---
    const mapLocation = ref('博多駅'); // 預設地圖地點
    const mapUrl = computed(() => {
        // 使用 Google Maps 舊版 Embed 格式 (免 API Key 快速測試用)
        // 如果有 API Key，建議改用: `https://www.google.com/maps/embed/v1/place?key=你的KEY&q=${mapLocation.value}`
        return `https://maps.google.com/maps?q=${encodeURIComponent(mapLocation.value)}&z=15&output=embed`;
    });

    // --- 切換到地圖並顯示地點 ---
    const showOnMap = (location) => {
        mapLocation.value = location; // 更新地圖地點
        currentTab.value = 'map';     // 切換分頁
    };

    // 用來即時預覽「新增視窗」裡的地圖
    const newLocationPreviewUrl = computed(() => {
        if (!newItem.value.location) return '';
        // 當使用者輸入地點時，自動產生 Google Maps Embed 連結
        return `https://www.google.com/maps?q=${encodeURIComponent(newItem.value.location)}&output=embed`;
    });

    // 資料狀態
    const itineraries = ref([]);// 記錄所有行程資料
    const expenses = ref([]);

    // 新增表單資料
    const newItem = ref({ time: '12:00', location: '', note: '' });
    const newExpense = ref({ itemName: '', amount: '', payerName: 'Me' });

    // 格式化日期給 API 用
    const formatDate = (dateStr) => {
        return new Date(dateStr).toISOString();
    };

    // API: 讀取行程
    const fetchItineraries = async () => {
        try {
            const res = await axios.get(`/api/itinerary?date=${currentDate.value}&user=${currentUser.value}`);
            itineraries.value = res.data;
        } catch (err) {
            console.error(err);
        }
    };

    // 合併「一般行程」與「飛機行程」並依照時間排序
    const sortedItinerary = computed(() => {
        let list = [];

        // 把後端抓來的行程加進去 (加上 type: 'normal' 標記)
        itineraries.value.forEach(item => {
            list.push({
                ...item,
                type: 'normal',
                sortTime: item.time // 統一用來排序的時間欄位
            });
        });

        const outboundDate = flightInfo.outbound.date ? flightInfo.outbound.date.split('T')[0] : '';
        const inboundDate = flightInfo.inbound.date ? flightInfo.inbound.date.split('T')[0] : '';

        // 檢查是否要加「去程飛機」 (加上 type: 'flight' 標記)
        if (currentDate.value === outboundDate) {
            list.push({
                type: 'flight',
                mode: 'outbound', // 標記是去程
                sortTime: flightInfo.outbound.departureTime, // 用起飛時間排序
                data: flightInfo.outbound // 把整包資料帶進去
            });
        }

        // 檢查是否要加「回程飛機」
        if (currentDate.value === inboundDate) {
            list.push({
                type: 'flight',
                mode: 'inbound', // 標記是回程
                sortTime: flightInfo.inbound.departureTime,
                data: flightInfo.inbound
            });
        }

        // 依照時間排序 (由早到晚)
        return list.sort((a, b) => {
            // 防止時間空白導致錯誤
            const timeA = a.sortTime || '00:00';
            const timeB = b.sortTime || '00:00';
            return timeA.localeCompare(timeB);
        });
    });

    // API: 新增行程
    const saveItem = async () => {
        // 1. 把勾選的陣列變成逗號分隔字串
        const participantsStr = selectedParticipants.value.join(',');

        try {
            const payload = {
                // ... 原本的欄位 ...
                time: newItem.value.time,
                location: newItem.value.location,
                note: newItem.value.note,

                // 加上參加者
                participants: participantsStr
            };

            if (isEditing.value) {
                await axios.put(`/api/itinerary/${newItem.value.id}`, payload);
            } else {
                await axios.post('/api/itinerary', payload);
            }

            // ... 重整與關閉 ...
        } catch (err) { /* ... */ }
    };

    // 開啟新增視窗 (重置表單)
    const openAddModal = () => {
        selectedParticipants.value = [...allMembers.value];

        isEditing.value = false;
        resetForm();
        showAddModal.value = true;
    };

    // 開啟編輯視窗 (帶入資料)
    const openEditModal = (item) => {
        isEditing.value = true;
        // 複製資料到表單 (避免直接修改影響畫面)
        newItem.value = { ...item };
        // 注意：item.time 可能是 "12:00:00"，input type="time" 只需要 "12:00"
        if (newItem.value.time.length > 5) {
            newItem.value.time = newItem.value.time.substring(0, 5);
        }
        showAddModal.value = true;
    };

    // 重置表單 Helper
    const resetForm = () => {
        newItem.value = { id: 0, time: '12:00', location: '', note: '' };
    };

    // API: 刪除行程
    const deleteItem = async (id) => {
        if (!confirm("確定刪除?")) return;
        try {
            await axios.delete(`/api/itinerary/${id}`);
            await fetchItineraries();
        } catch (err) {
            console.error(err);
        }
    };

    // API: 讀取帳目
    const fetchExpenses = async () => {
        const res = await axios.get('/api/expense');
        expenses.value = res.data;
    };

    // API: 新增帳目
    const addExpense = async () => {
        if (!newExpense.value.itemName) return;
        await axios.post('/api/expense', newExpense.value);
        newExpense.value = { itemName: '', amount: '', payerName: 'Me' };
        await fetchExpenses();
    };

    // 監聽日期改變
    watch(currentDate, () => {
        fetchItineraries();
    });

    onMounted(() => {
        fetchItineraries();
        fetchExpenses();
    });

    // 輔助函式
    const getDayName = (d) => ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'][new Date(d).getDay()];
    const getDayNum = (d) => d.split('-')[2];

    // 只顯示日期 (去掉 T00:00:00)
    const formatDateOnly = (val) => {
        if (!val) return '';
        return val.toString().split('T')[0];
    };

    // 計算分帳
    const splitResult = computed(() => {
        let myTotal = 0, friendTotal = 0;
        expenses.value.forEach(e => {
            if (e.payerName === 'Me' || e.payerName === '我') myTotal += Number(e.amount);
            else friendTotal += Number(e.amount);
        });
        const diff = (myTotal - friendTotal) / 2;
        if (diff > 0) return `朋友應給你 <span class="text-yellow-400 font-bold">¥${diff}</span>`;
        if (diff < 0) return `你應給朋友 <span class="text-yellow-400 font-bold">¥${Math.abs(diff)}</span>`;
        return "帳目已平！";
    });
</script>

<template>
    <div class="flex justify-center min-h-screen items-center font-sans text-gray-600 bg-gray-100">

        <div v-if="!isLoggedIn" class="w-full max-w-sm bg-white p-8 rounded-2xl shadow-xl">
            <h2 class="text-2xl font-bold text-lake-dark text-center mb-6">
                {{ isRegisterMode ? '註冊帳號' : '歡迎回來' }}
            </h2>

            <div class="space-y-4">
                <div>
                    <label class="text-xs font-bold text-gray-400">帳號</label>
                    <input v-model="authForm.username" type="text" class="w-full p-3 bg-gray-50 rounded-xl border focus:border-primary outline-none">
                </div>
                <div>
                    <label class="text-xs font-bold text-gray-400">密碼</label>
                    <input v-model="authForm.password" type="password" class="w-full p-3 bg-gray-50 rounded-xl border focus:border-primary outline-none">
                </div>

                <button @click="handleAuth" class="w-full bg-primary text-white py-3 rounded-xl font-bold hover:bg-lake-dark transition shadow-lg shadow-primary/30">
                    {{ isRegisterMode ? '註冊' : '登入' }}
                </button>

                <div class="text-center text-sm text-gray-400 mt-4">
                    {{ isRegisterMode ? '已經有帳號?' : '還沒有帳號?' }}
                    <span @click="isRegisterMode = !isRegisterMode" class="text-primary font-bold cursor-pointer hover:underline">
                        {{ isRegisterMode ? '登入' : '去註冊' }}
                    </span>
                </div>

                <div class="relative my-4">
                    <div class="absolute inset-0 flex items-center">
                        <div class="w-full border-t border-gray-200"></div>
                    </div>
                    <div class="relative flex justify-center text-sm">
                        <span class="px-2 bg-white text-gray-400">Or continue with</span>
                    </div>
                </div>

                <button @click="handleGoogleLogin"
                        class="w-full flex items-center justify-center gap-2 bg-white border border-gray-300 text-gray-700 py-3 rounded-xl font-bold hover:bg-gray-50 transition">
                    <img src="https://www.svgrepo.com/show/475656/google-color.svg" class="w-5 h-5" alt="google logo">
                    Google 登入
                </button>
            </div>
        </div>

        <div v-else class="w-full flex justify-center">

            <div v-if="!currentTripId" class="w-full max-w-4xl px-4 animate-fade-in">
                <div class="flex justify-between items-center mb-6 md:mb-8 bg-white p-3 md:p-4 rounded-2xl shadow-sm">

                    <div class="flex items-center gap-2 md:gap-3 overflow-hidden">
                        <div class="w-8 h-8 md:w-10 md:h-10 bg-primary text-white rounded-full flex items-center justify-center font-bold text-sm md:text-xl flex-shrink-0">
                            {{ currentUser[0]?.toUpperCase() }}
                        </div>
                        <div class="min-w-0">
                            <div class="text-[10px] md:text-xs text-gray-400 truncate">Welcome</div>
                            <div class="font-bold text-sm md:text-base text-gray-700 truncate max-w-[100px] md:max-w-none">
                                {{ currentUser }}
                            </div>
                        </div>
                    </div>

                    <div class="flex gap-2 md:gap-3 flex-shrink-0">
                        <button @click="logout" class="w-8 h-8 md:w-auto md:px-4 md:py-2 text-red-400 hover:bg-red-50 rounded-xl transition font-bold text-sm flex items-center justify-center border border-gray-100 md:border-none">
                            <i class="fa-solid fa-right-from-bracket"></i>
                            <span class="hidden md:inline ml-1">登出</span>
                        </button>

                        <button @click="openCreateTripModal" class="px-3 py-2 md:px-4 bg-primary text-white rounded-xl shadow-sm hover:bg-lake-dark transition font-bold text-xs md:text-sm flex items-center whitespace-nowrap">
                            <i class="fa-solid fa-plus mr-1"></i>
                            <span>新增</span>
                            <span class="hidden md:inline">旅遊</span>
                        </button>
                    </div>
                </div>

                <h2 class="text-2xl font-bold text-lake-dark mb-6 pl-2 border-l-4 border-primary">我的旅遊計畫</h2>

                <div v-if="myTrips.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    <div v-for="trip in myTrips" :key="trip.id"
                         @click="selectTrip(trip)"
                         class="bg-white p-4 md:p-6 rounded-3xl shadow-sm hover:shadow-xl cursor-pointer transition-all hover:-translate-y-1 border-2 border-transparent hover:border-primary group relative overflow-hidden">

                        <div class="relative z-10">
                            <div class="flex justify-between items-start mb-2">
                                <h3 class="text-lg md:text-xl font-bold text-gray-800 line-clamp-1 break-all">
                                    {{ trip.title }}
                                </h3>

                                <div class="flex gap-1 md:gap-2 flex-shrink-0 ml-2">
                                    <button v-if="trip.ownerId === currentUserId"
                                            @click="openEditTripModal(trip, $event)"
                                            class="p-2 text-gray-400 hover:text-blue-500 transition rounded-full hover:bg-blue-50">
                                        <i class="fa-solid fa-pen text-sm md:text-base"></i>
                                    </button>

                                    <button @click="deleteTrip(trip, $event)"
                                            class="p-2 text-gray-400 hover:text-red-500 transition rounded-full hover:bg-red-50">
                                        <i class="fa-solid fa-trash text-sm md:text-base"></i>
                                    </button>
                                </div>
                            </div>

                            <div class="text-gray-400 text-xs md:text-sm mb-4 font-medium flex items-center">
                                <i class="fa-regular fa-calendar mr-2"></i>
                                <span class="truncate">{{ formatDateOnly(trip.startDate) }} ~ {{ formatDateOnly(trip.endDate) }}</span>
                            </div>

                            <div class="flex justify-between items-end mt-2 md:mt-4">
                                <div class="flex -space-x-2 overflow-hidden">
                                    <div v-for="(p, idx) in (trip.travelGroup?.members || []).slice(0, 5)" :key="idx"
                                         class="w-6 h-6 md:w-8 md:h-8 rounded-full bg-gray-100 border-2 border-white flex items-center justify-center text-[10px] md:text-xs font-bold text-gray-500 shadow-sm">
                                        {{ p.username ? p.username[0]?.toUpperCase() : '?' }}
                                    </div>

                                    <div v-if="(trip.travelGroup?.members || []).length > 5" class="w-6 h-6 md:w-8 md:h-8 rounded-full bg-gray-50 border-2 border-white flex items-center justify-center text-[10px] text-gray-400">
                                        +{{ (trip.travelGroup?.members || []).length - 5 }}
                                    </div>
                                </div>
                                <span class="text-primary font-bold text-xs md:text-sm group-hover:translate-x-1 transition whitespace-nowrap">
                                    GO <i class="fa-solid fa-arrow-right"></i>
                                </span>
                            </div>
                        </div>

                        <div class="absolute -right-4 -top-4 w-20 h-20 md:w-24 md:h-24 bg-blue-50 rounded-full group-hover:scale-150 transition duration-500"></div>
                    </div>
                </div>

                <div v-else class="text-center py-20 bg-white rounded-3xl shadow-sm border border-dashed border-gray-300">
                    <div class="text-6xl mb-4">🌏</div>
                    <h3 class="text-xl font-bold text-gray-400">還沒有任何計畫</h3>
                    <p class="text-gray-400 text-sm mb-6">點擊右上方按鈕開始你的第一趟旅程吧！</p>
                </div>
            </div>

            <div v-else class="w-full max-w-md h-[90vh] bg-soft-gray shadow-2xl sm:rounded-[40px] overflow-hidden relative flex flex-col border-4 border-white animate-slide-up">

                <header class="bg-white p-6 pb-2 shadow-sm z-10">
                    <div class="flex justify-between items-center mb-4">
                        <button @click="backToTrips" class="text-gray-400 hover:text-lake-dark transition flex items-center gap-1 font-bold text-sm">
                            <i class="fa-solid fa-chevron-left"></i> 列表
                        </button>

                        <div class="flex items-center gap-2">
                            <span class="text-xs bg-gray-100 px-2 py-1 rounded text-gray-500">
                                <i class="fa-solid fa-user text-primary mr-1"></i> {{ currentUser }}
                            </span>
                        </div>
                    </div>

                    <div class="flex justify-between items-center mb-4 px-1">
                        <div>
                            <h1 class="text-2xl font-bold tracking-widest text-lake-dark">
                                {{ myTrips.find(t => t.id === currentTripId)?.title || 'TRIP' }}
                            </h1>
                            <div class="text-xs text-gray-400">Travel Plan</div>
                        </div>
                    </div>

                    

                    <div class="flex overflow-x-auto gap-3 px-4 py-2 scrollbar-hide">
                        <div v-for="day in dates" :key="day" @click="currentDate = day"
                             :class="['flex-shrink-0 w-14 h-20 rounded-2xl flex flex-col justify-center items-center transition border cursor-pointer',
                                      currentDate === day ? 'bg-primary text-white shadow-lg -translate-y-1' : 'bg-white text-gray-400']">
                            <span class="text-xs">{{ getDayName(day) }}</span>
                            <span class="text-xl font-bold">{{ getDayNum(day) }}</span>
                        </div>
                    </div>
                </header>

                <main class="flex-1 overflow-y-auto hide-scrollbar p-5 pb-24">
                    <div class="px-4 mb-4">
                        <div class="bg-white rounded-2xl shadow-sm p-4 border border-gray-100">
                            <div class="flex justify-between items-center mb-3 pb-2 border-b border-gray-100">
                                <h3 class="font-bold text-gray-700 flex items-center gap-2">
                                    ✈️ 機票資訊
                                </h3>
                                <button @click="toggleEdit" class="text-sm text-primary font-medium hover:text-blue-600 transition">
                                    {{ isEditingFlight ? '完成' : '編輯' }}
                                </button>
                            </div>

                            <div v-if="isEditingFlight" class="space-y-6 animate-fade-in">

                                <div class="bg-gray-50 p-4 rounded-xl border border-blue-100">
                                    <div class="flex justify-between items-center mb-4">
                                        <span class="bg-blue-100 text-primary text-xs font-bold px-2 py-1 rounded">去程 (Outbound)</span>
                                        <button @click="addSegment('Outbound')" class="text-xs flex items-center gap-1 text-blue-500 hover:text-blue-700 font-bold">
                                            <i class="fa-solid fa-plus"></i> 加入轉機
                                        </button>
                                    </div>

                                    <div v-for="(flight, index) in flightInfo.outbound" :key="index" class="relative mb-6 pb-6 border-b border-dashed border-gray-300 last:border-0 last:mb-0 last:pb-0">

                                        <button v-if="flightInfo.outbound.length > 1" @click="removeSegment(flight, flightInfo.outbound, index)" class="absolute -right-2 -top-2 text-gray-300 hover:text-red-400">
                                            <i class="fa-solid fa-circle-minus"></i>
                                        </button>

                                        <div class="grid grid-cols-2 gap-3 mb-3">
                                            <div class="mb-3">
                                                <label class="text-[10px] font-bold text-gray-400 ml-1">日期</label>
                                                <input v-model="flight.date" type="datetime-local" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-primary outline-none">
                                            </div>

                                            <div class="grid grid-cols-2 gap-3 mb-3">
                                                <div>
                                                    <label class="text-[10px] font-bold text-gray-400 ml-1">航空公司</label>
                                                    <input v-model="flight.airline" placeholder="例: 長榮" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-primary outline-none">
                                                </div>
                                                <div>
                                                    <label class="text-[10px] font-bold text-gray-400 ml-1">航班編號</label>
                                                    <input v-model="flight.number" placeholder="BR198" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-primary outline-none uppercase">
                                                </div>
                                            </div>
                                        </div>

                                        <div class="grid grid-cols-2 gap-3 mb-3">
                                            <div class="relative">
                                                <label class="text-[10px] font-bold text-gray-400 ml-1">出發地</label>
                                                <input v-model="flight.departure" placeholder="TPE" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-primary outline-none uppercase pl-8">
                                                <i class="fa-solid fa-plane-departure absolute left-3 top-8 text-gray-300 text-xs"></i>
                                            </div>
                                            <div class="relative">
                                                <label class="text-[10px] font-bold text-gray-400 ml-1">抵達地</label>
                                                <input v-model="flight.arrival" placeholder="NRT" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-primary outline-none uppercase pl-8">
                                                <i class="fa-solid fa-plane-arrival absolute left-3 top-8 text-gray-300 text-xs"></i>
                                            </div>
                                        </div>

                                        <div class="grid grid-cols-2 gap-3">
                                            <div>
                                                <label class="text-[10px] font-bold text-gray-400 ml-1">起飛時間</label>
                                                <input v-model="flight.departureTime" type="time" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-primary outline-none">
                                            </div>
                                            <div>
                                                <label class="text-[10px] font-bold text-gray-400 ml-1">抵達時間</label>
                                                <input v-model="flight.arrivalTime" type="time" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-primary outline-none">
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <div class="bg-gray-50 p-4 rounded-xl border border-orange-100">
                                    <div class="flex justify-between items-center mb-4">
                                        <span class="bg-orange-100 text-orange-500 text-xs font-bold px-2 py-1 rounded">回程 (Inbound)</span>
                                        <button @click="addSegment('Inbound')" class="text-xs flex items-center gap-1 text-orange-500 hover:text-orange-700 font-bold">
                                            <i class="fa-solid fa-plus"></i> 加入轉機
                                        </button>
                                    </div>

                                    <div v-for="(flight, index) in flightInfo.inbound" :key="index" class="relative mb-6 pb-6 border-b border-dashed border-gray-300 last:border-0 last:mb-0 last:pb-0">

                                        <button v-if="flightInfo.inbound.length > 1" @click="removeSegment(flight, flightInfo.inbound, index)" class="absolute -right-2 -top-2 text-gray-300 hover:text-red-400">
                                            <i class="fa-solid fa-circle-minus"></i>
                                        </button>

                                        <div class="grid grid-cols-2 gap-3 mb-3">
                                            <div class="mb-3">
                                                <label class="text-[10px] font-bold text-gray-400 ml-1">日期</label>
                                                <input v-model="flight.date" type="datetime-local" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-orange-400 outline-none">
                                            </div>

                                            <div class="grid grid-cols-2 gap-3 mb-3">
                                                <div>
                                                    <label class="text-[10px] font-bold text-gray-400 ml-1">航空公司</label>
                                                    <input v-model="flight.airline" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-orange-400 outline-none">
                                                </div>
                                                <div>
                                                    <label class="text-[10px] font-bold text-gray-400 ml-1">航班編號</label>
                                                    <input v-model="flight.number" placeholder="BR198" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-orange-400 outline-none uppercase">
                                                </div>
                                            </div>
                                        </div>

                                        <div class="grid grid-cols-2 gap-3 mb-3">
                                            <div class="relative">
                                                <label class="text-[10px] font-bold text-gray-400 ml-1">出發地</label>
                                                <input v-model="flight.departure" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-orange-400 outline-none uppercase pl-8">
                                                <i class="fa-solid fa-plane-departure absolute left-3 top-8 text-gray-300 text-xs"></i>
                                            </div>
                                            <div class="relative">
                                                <label class="text-[10px] font-bold text-gray-400 ml-1">抵達地</label>
                                                <input v-model="flight.arrival" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-orange-400 outline-none uppercase pl-8">
                                                <i class="fa-solid fa-plane-arrival absolute left-3 top-8 text-gray-300 text-xs"></i>
                                            </div>
                                        </div>

                                        <div class="grid grid-cols-2 gap-3">
                                            <div>
                                                <label class="text-[10px] font-bold text-gray-400 ml-1">起飛時間</label>
                                                <input v-model="flight.departureTime" type="time" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-orange-400 outline-none">
                                            </div>
                                            <div>
                                                <label class="text-[10px] font-bold text-gray-400 ml-1">抵達時間</label>
                                                <input v-model="flight.arrivalTime" type="time" class="w-full p-2 bg-white rounded-lg border text-sm focus:border-orange-400 outline-none">
                                            </div>
                                        </div>
                                    </div>
                                </div>

                            </div>

                            <div v-else class="space-y-3">
                                <div v-for="(flight, idx) in flightInfo.outbound" :key="'out-'+idx" class="flex items-center justify-between">
                                    <div class="flex flex-col">
                                        <span class="text-xs text-gray-400">{{ flight.departure || 'DEP' }}</span>
                                        <span class="font-bold text-lg text-gray-800">{{ flight.departureTime || '--:--' }}</span>
                                    </div>
                                    <div class="flex flex-col items-center px-2 w-20">
                                        <div class="text-[10px] text-gray-400 mb-1">{{ flight.airline }}</div>
                                        <div class="flex items-center w-full">
                                            <div class="h-[1px] bg-gray-300 flex-1"></div>
                                            <i class="fa-solid fa-plane text-primary text-xs mx-1"></i>
                                            <div class="h-[1px] bg-gray-300 flex-1"></div>
                                        </div>
                                        <div v-if="idx < flightInfo.outbound.length - 1" class="text-[9px] text-blue-400 mt-1">轉機</div>
                                    </div>
                                    <div class="flex flex-col items-end">
                                        <span class="text-xs text-gray-400">{{ flight.arrival || 'ARR' }}</span>
                                        <span class="font-bold text-lg text-gray-800">{{ flight.arrivalTime || '--:--' }}</span>
                                    </div>
                                </div>

                                <div class="border-t border-dashed border-gray-200 my-2"></div>

                                <div v-for="(flight, idx) in flightInfo.inbound" :key="'in-'+idx" class="flex items-center justify-between">
                                    <div class="flex flex-col">
                                        <span class="text-xs text-gray-400">{{ flight.departure || 'DEP' }}</span>
                                        <span class="font-bold text-lg text-gray-800">{{ flight.departureTime || '--:--' }}</span>
                                    </div>
                                    <div class="flex flex-col items-center px-2 w-20">
                                        <div class="text-[10px] text-gray-400 mb-1">{{ flight.airline }}</div>
                                        <div class="flex items-center w-full">
                                            <div class="h-[1px] bg-gray-300 flex-1"></div>
                                            <i class="fa-solid fa-plane text-orange-400 text-xs mx-1 rotate-180"></i>
                                            <div class="h-[1px] bg-gray-300 flex-1"></div>
                                        </div>
                                        <div v-if="idx < flightInfo.inbound.length - 1" class="text-[9px] text-orange-400 mt-1">轉機</div>
                                    </div>
                                    <div class="flex flex-col items-end">
                                        <span class="text-xs text-gray-400">{{ flight.arrival || 'ARR' }}</span>
                                        <span class="font-bold text-lg text-gray-800">{{ flight.arrivalTime || '--:--' }}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div v-if="currentTab === 'itinerary'">
                        <div class="flex justify-between mb-4">
                            <h2 class="text-xl font-bold">每日行程</h2>
                            <button @click="openAddModal" class="text-primary font-bold hover:text-lake-dark transition">
                                <i class="fa-solid fa-plus-circle"></i> 新增
                            </button>
                        </div>

                        <div v-if="itineraries.length === 0" class="text-center py-10 opacity-50">本日尚無行程</div>

                        <div v-for="(item, index) in sortedItinerary" :key="index" class="mb-4">
                            <div class="bg-white p-4 rounded-xl shadow-sm border-l-4 border-primary">
                                <div class="flex justify-between">
                                    <span class="font-bold">{{ (item.time || '').substring(0,5) }}</span>
                                    <span>{{ item.location }}</span>
                                </div>
                                <div class="text-xs text-gray-400">{{ item.note }}</div>
                                <div class="flex gap-2 mt-2 justify-end">
                                    <button @click.stop="openEditModal(item)" class="text-blue-400"><i class="fa-solid fa-pen"></i></button>
                                    <button @click.stop="deleteItem(item.id)" class="text-red-400"><i class="fa-solid fa-trash"></i></button>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div v-if="currentTab === 'map'" class="h-full">
                        <h2 class="text-xl font-bold mb-4">地圖導航</h2>
                        <iframe width="100%" height="80%" frameborder="0" style="border:0" :src="mapUrl" allowfullscreen class="rounded-2xl border-2 border-white shadow-sm"></iframe>
                    </div>

                    <div v-if="currentTab === 'expenses'">
                        <h2 class="text-xl font-bold mb-4">分帳助手</h2>
                        <div class="bg-white p-4 rounded-2xl shadow-sm mb-4">
                            <div class="grid grid-cols-2 gap-2 mb-2">
                                <input v-model="newExpense.itemName" placeholder="項目" class="bg-gray-50 p-2 rounded col-span-2">
                                <input v-model="newExpense.amount" type="number" placeholder="金額" class="bg-gray-50 p-2 rounded">
                                <select v-model="newExpense.payerName" class="bg-gray-50 p-2 rounded">
                                    <option value="Me">我付</option>
                                    <option value="Friend">朋友付</option>
                                </select>
                            </div>
                            <button @click="addExpense" class="w-full bg-primary text-white py-2 rounded font-bold">記帳</button>
                        </div>
                        <div class="space-y-2">
                            <div v-for="exp in expenses" :key="exp.id" class="flex justify-between bg-white p-3 rounded-xl shadow-sm">
                                <div>{{ exp.itemName }} <span class="text-xs text-gray-400">({{ exp.payerName }})</span></div>
                                <div class="font-bold">¥{{ exp.amount }}</div>
                            </div>
                        </div>
                        <div class="mt-4 bg-lake-dark text-white p-4 rounded-xl" v-html="splitResult"></div>
                    </div>
                </main>

                <nav class="absolute bottom-0 w-full bg-white py-4 flex justify-around border-t z-20">
                    <button @click="currentTab='itinerary'" :class="['flex flex-col items-center space-y-1 transition', currentTab==='itinerary'?'text-primary':'text-gray-300']">
                        <i class="fa-solid fa-list-ul text-xl"></i>
                    </button>
                    <button @click="currentTab='map'" :class="['flex flex-col items-center space-y-1 transition', currentTab==='map'?'text-primary':'text-gray-300']">
                        <i class="fa-solid fa-map-location-dot text-xl"></i>
                    </button>
                    <button @click="currentTab='expenses'" :class="['flex flex-col items-center space-y-1 transition', currentTab==='expenses'?'text-primary':'text-gray-300']">
                        <i class="fa-solid fa-calculator text-xl"></i>
                    </button>
                </nav>

                <div v-if="showAddModal && currentTripId" class="absolute inset-0 bg-black/30 flex items-center justify-center z-50 p-6 backdrop-blur-sm">
                    <div class="bg-white w-full rounded-2xl p-6 shadow-2xl max-h-[90vh] overflow-y-auto hide-scrollbar">
                        <h3 class="font-bold mb-4 text-lake-dark text-lg">{{ isEditing ? '編輯行程' : '新增行程' }}</h3>

                        <label class="text-xs text-gray-400 font-bold ml-1">參加人員</label>
                        <div class="flex flex-wrap gap-2 mb-3 bg-gray-50 p-2 rounded-xl border">
                            <label v-for="member in allMembers" :key="member" class="flex items-center space-x-1 cursor-pointer select-none px-2 py-1 rounded transition" :class="selectedParticipants.includes(member) ? 'bg-blue-100 text-blue-600' : 'text-gray-400'">
                                <input type="checkbox" :value="member" v-model="selectedParticipants" class="hidden">
                                <span class="text-sm font-bold">{{ member }}</span>
                                <i v-if="selectedParticipants.includes(member)" class="fa-solid fa-check text-xs"></i>
                            </label>
                        </div>

                        <label class="text-xs text-gray-400 font-bold ml-1">時間</label>
                        <input v-model="newItem.time" type="time" class="w-full bg-gray-50 p-3 rounded-xl mb-3 border">
                        <label class="text-xs text-gray-400 font-bold ml-1">地點</label>
                        <input v-model="newItem.location" class="w-full bg-gray-50 p-3 rounded-xl mb-3 border">
                        <label class="text-xs text-gray-400 font-bold ml-1">備註</label>
                        <input v-model="newItem.note" class="w-full bg-gray-50 p-3 rounded-xl mb-6 border">

                        <div class="flex gap-3">
                            <button @click="showAddModal=false" class="flex-1 bg-gray-100 text-gray-500 py-3 rounded-xl font-bold">取消</button>
                            <button @click="saveItem" class="flex-1 bg-primary text-white py-3 rounded-xl font-bold">確認</button>
                        </div>
                    </div>
                </div>

            </div>
        </div> 
        <div v-if="showCreateTripModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 backdrop-blur-sm animate-fade-in">
            <div class="bg-white w-full max-w-md p-6 rounded-3xl shadow-2xl m-4">
                <div class="flex justify-between items-center mb-6">
                    <h3 class="text-xl font-bold text-lake-dark">
                        {{ isEditingTrip ? '編輯旅程' : '建立新旅程' }}
                    </h3>
                    <button @click="showCreateTripModal=false" class="text-gray-400 hover:text-gray-600"><i class="fa-solid fa-xmark text-xl"></i></button>
                </div>

                <div class="space-y-4">
                    <div>
                        <label class="text-xs font-bold text-gray-400 ml-1">旅遊名稱</label>
                        <input v-model="newTripForm.title" placeholder="例如: 東京五天四夜" class="w-full p-3 bg-gray-50 rounded-xl border focus:border-primary outline-none transition">
                    </div>

                    <div class="grid grid-cols-2 gap-4">
                        <div>
                            <label class="text-xs font-bold text-gray-400 ml-1">開始日期</label>
                            <input v-model="newTripForm.startDate" type="date" class="w-full p-3 bg-gray-50 rounded-xl border focus:border-primary outline-none">
                        </div>
                        <div>
                            <label class="text-xs font-bold text-gray-400 ml-1">結束日期</label>
                            <input v-model="newTripForm.endDate" type="date" class="w-full p-3 bg-gray-50 rounded-xl border focus:border-primary outline-none">
                        </div>
                    </div>

                    <div>
                        <label class="text-xs font-bold text-gray-400 ml-1">旅伴 (複選)</label>
                        <div class="bg-gray-50 p-3 rounded-xl border max-h-32 overflow-y-auto">
                            <div v-if="allMembers.length === 0" class="text-xs text-gray-400">載入中...</div>
                            <div v-for="member in allMembers" :key="member" class="flex items-center mb-2 last:mb-0">
                                <input type="checkbox" :id="`m-${member}`" :value="member" v-model="newTripForm.participants" class="w-4 h-4 text-primary rounded border-gray-300 focus:ring-primary">
                                <label :for="`m-${member}`" class="ml-2 text-sm text-gray-600 cursor-pointer select-none">{{ member }}</label>
                            </div>
                        </div>
                    </div>

                    <button @click="submitTripForm" class="w-full bg-primary text-white py-3 rounded-xl font-bold shadow-lg shadow-primary/30 hover:bg-lake-dark transition mt-4">
                        {{ isEditingTrip ? '儲存變更' : '開始規劃 ✈️' }}
                    </button>
                </div>
            </div>
        </div>

    </div>
</template>