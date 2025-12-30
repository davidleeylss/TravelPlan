<script setup>
    import { ref, computed, onMounted, watch, reactive } from 'vue';
    import axios from 'axios';

    // 定義目前的使用者 (預設 UserA)
    const currentUser = ref('UserA');
    const users = ['UserA', 'UserB', 'UserC']; // 模擬使用者列表

    // 定義這趟旅程的所有成員
    const allMembers = ['UserA', 'UserB', 'UserC', 'UserD', 'UserE', 'UserF'];
    // 目前正在編輯的行程，有哪些參加者 (預設全選)
    const selectedParticipants = ref([...allMembers]);

    // 機票資料 (改回預設空值，等待 API 填入)
    const flightInfo = reactive({
        outbound: {
            id: 0,
            type: 'Outbound', // 標記類型
            participants: '',
            date: '',
            departureTime: '',
            arrivalTime: '',
            departure: '',
            arrival: '',
            airline: '',
            number: '',
        },
        inbound: {
            id: 0,
            type: 'Inbound', // 標記類型
            participants: '',
            date: '',
            departureTime: '',
            arrivalTime: '',
            departure: '',
            arrival: '',
            airline: '',
            number: '',
        }
    });

    // API: 讀取機票
    const fetchFlights = async () => {
        try {
            const res = await axios.get(`/api/flight?user=${currentUser.value}`);
            const data = res.data;

            // 如果資料庫有資料，就填入 flightInfo
            const outboundData = data.find(f => f.type === 'Outbound');
            if (outboundData) Object.assign(flightInfo.outbound, outboundData);

            const inboundData = data.find(f => f.type === 'Inbound');
            if (inboundData) Object.assign(flightInfo.inbound, inboundData);

        } catch (err) {
            console.error("讀取機票失敗", err);
        }
    };

    // API: 儲存機票 (修改 toggleEdit 函式)
    const toggleEdit = async () => {
        if (isEditingFlight.value) {
            // 如果原本是「編輯中」，現在按下「完成」，則觸發儲存
            try {
                // 處理去程
                await axios.post('/api/flight', {
                    ...flightInfo.outbound,
                    participants: flightInfo.outbound.participants || currentUser.value
                });

                // 處理回程
                await axios.post('/api/flight', {
                    ...flightInfo.inbound,
                    participants: flightInfo.inbound.participants || currentUser.value
                });

                alert('機票資訊已儲存！');
                fetchFlights(); // 存完重抓一次，確保 Id 更新
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

    onMounted(() => {
        fetchFlights();
        // fetchItineraries();
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

                // 🌟 關鍵：加上參加者
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
        selectedParticipants.value = [...allMembers];

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
    <div class="flex justify-center min-h-screen items-center font-sans text-gray-600">
        <div class="w-full max-w-md h-[90vh] bg-soft-gray shadow-2xl sm:rounded-[40px] overflow-hidden relative flex flex-col border-4 border-white">

            <header class="bg-white p-6 pb-2 shadow-sm z-10">
                <div class="flex justify-end mb-2">
                    <select v-model="currentUser" class="bg-gray-100 text-sm p-1 rounded border border-gray-300">
                        <option v-for="u in users" :key="u" :value="u">我是 {{ u }}</option>
                    </select>
                </div>

                <div class="flex justify-between items-center mb-4">
                    <h1 class="text-2xl font-bold tracking-widest text-lake-dark">FUKUOKA</h1>
                    <div class="text-xs text-gray-400">福岡之旅</div>
                </div>
                <div class="px-4 mb-4">
                    <div class="bg-white rounded-2xl shadow-sm p-4 border border-gray-100">

                        <div class="flex justify-between items-center mb-3 pb-2 border-b border-gray-100">
                            <h3 class="font-bold text-gray-700 flex items-center gap-2">
                                ✈️ 機票資訊
                            </h3>
                            <button @click="toggleEdit"
                                    class="text-sm text-primary font-medium hover:text-blue-600 transition">
                                {{ isEditingFlight ? '完成' : '編輯' }}
                            </button>
                        </div>

                        <div v-if="isEditingFlight" class="space-y-4">

                            <div class="bg-gray-50 p-3 rounded-lg border border-gray-200">
                                <div class="text-xs text-primary font-bold mb-2 border-b border-gray-200 pb-1">去程 (Outbound)</div>

                                <div class="grid grid-cols-2 gap-2 mb-2">
                                    <div>
                                        <label class="text-[10px] text-gray-400 font-bold">出發地</label>
                                        <input v-model="flightInfo.outbound.departure" class="border rounded p-1 text-sm w-full">
                                    </div>
                                    <div>
                                        <label class="text-[10px] text-gray-400 font-bold">目的地</label>
                                        <input v-model="flightInfo.outbound.arrival" class="border rounded p-1 text-sm w-full">
                                    </div>
                                </div>

                                <div class="mb-2">
                                    <label class="text-[10px] text-gray-400 font-bold">日期</label>
                                    <input v-model="flightInfo.outbound.date" type="date" class="border rounded p-1 text-sm w-full bg-white">
                                </div>

                                <div class="grid grid-cols-2 gap-2 mb-2">
                                    <div>
                                        <label class="text-[10px] text-gray-400 font-bold">起飛時間</label>
                                        <input v-model="flightInfo.outbound.departureTime" type="time" class="border rounded p-1 text-sm w-full bg-white">
                                    </div>
                                    <div>
                                        <label class="text-[10px] text-gray-400 font-bold">抵達時間</label>
                                        <input v-model="flightInfo.outbound.arrivalTime" type="time" class="border rounded p-1 text-sm w-full bg-white">
                                    </div>
                                </div>

                                <div class="grid grid-cols-2 gap-2">
                                    <div>
                                        <label class="text-[10px] text-gray-400 font-bold">航空公司</label>
                                        <input v-model="flightInfo.outbound.airline" class="border rounded p-1 text-sm w-full">
                                    </div>
                                    <div>
                                        <label class="text-[10px] text-gray-400 font-bold">班機號碼</label>
                                        <input v-model="flightInfo.outbound.number" class="border rounded p-1 text-sm w-full">
                                    </div>
                                </div>
                            </div>

                            <div class="bg-gray-50 p-3 rounded-lg border border-gray-200">
                                <div class="text-xs text-primary font-bold mb-2 border-b border-gray-200 pb-1">回程 (Inbound)</div>

                                <div class="grid grid-cols-2 gap-2 mb-2">
                                    <div>
                                        <label class="text-[10px] text-gray-400 font-bold">出發地</label>
                                        <input v-model="flightInfo.inbound.departure" class="border rounded p-1 text-sm w-full">
                                    </div>
                                    <div>
                                        <label class="text-[10px] text-gray-400 font-bold">目的地</label>
                                        <input v-model="flightInfo.inbound.arrival" class="border rounded p-1 text-sm w-full">
                                    </div>
                                </div>

                                <div class="mb-2">
                                    <label class="text-[10px] text-gray-400 font-bold">日期</label>
                                    <input v-model="flightInfo.inbound.date" type="date" class="border rounded p-1 text-sm w-full bg-white">
                                </div>

                                <div class="grid grid-cols-2 gap-2 mb-2">
                                    <div>
                                        <label class="text-[10px] text-gray-400 font-bold">起飛時間</label>
                                        <input v-model="flightInfo.inbound.departureTime" type="time" class="border rounded p-1 text-sm w-full bg-white">
                                    </div>
                                    <div>
                                        <label class="text-[10px] text-gray-400 font-bold">抵達時間</label>
                                        <input v-model="flightInfo.inbound.arrivalTime" type="time" class="border rounded p-1 text-sm w-full bg-white">
                                    </div>
                                </div>

                                <div class="grid grid-cols-2 gap-2">
                                    <div>
                                        <label class="text-[10px] text-gray-400 font-bold">航空公司</label>
                                        <input v-model="flightInfo.inbound.airline" class="border rounded p-1 text-sm w-full">
                                    </div>
                                    <div>
                                        <label class="text-[10px] text-gray-400 font-bold">班機號碼</label>
                                        <input v-model="flightInfo.inbound.number" class="border rounded p-1 text-sm w-full">
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div v-else class="space-y-3">

                            <div class="flex items-center justify-between">
                                <div class="flex flex-col">
                                    <span class="text-xs text-gray-400">{{ flightInfo.outbound.departure }}</span>
                                    <span class="font-bold text-lg text-gray-800">{{ flightInfo.outbound.departureTime }}</span>
                                    <span class="text-xs bg-blue-100 text-blue-600 px-1.5 py-0.5 rounded w-fit mt-1">
                                        {{ formatDateOnly(flightInfo.outbound.date) }}
                                    </span>
                                </div>

                                <div class="flex flex-col items-center px-2">
                                    <span class="text-xs text-gray-400 mb-1">{{ flightInfo.outbound.number }}</span>
                                    <div class="flex items-center text-gray-300 text-xs">
                                        ----- <i class="fa-solid fa-plane mx-1"></i> -----
                                    </div>
                                    <span class="text-[10px] text-gray-400 mt-1">{{ flightInfo.outbound.airline }}</span>
                                </div>

                                <div class="flex flex-col items-end">
                                    <span class="text-xs text-gray-400">{{ flightInfo.outbound.arrival }}</span>
                                    <span class="font-bold text-lg text-gray-800">{{ flightInfo.outbound.arrivalTime }}</span>
                                    <span class="text-xs bg-blue-100 text-blue-600 px-1.5 py-0.5 rounded w-fit mt-1">
                                        {{ formatDateOnly(flightInfo.outbound.date) }}
                                    </span>
                                </div>
                            </div>

                            <div class="border-t border-dashed border-gray-200 my-2"></div>

                            <div class="flex items-center justify-between">
                                <div class="flex flex-col">
                                    <span class="text-xs text-gray-400">{{ flightInfo.inbound.departure }}</span>
                                    <span class="font-bold text-lg text-gray-800">{{ flightInfo.inbound.departureTime }}</span>
                                    <span class="text-xs bg-blue-100 text-blue-600 px-1.5 py-0.5 rounded w-fit mt-1">
                                        {{ formatDateOnly(flightInfo.inbound.date) }}
                                    </span>
                                </div>
                                <div class="flex flex-col items-center px-2">
                                    <span class="text-xs text-gray-400 mb-1">{{ flightInfo.inbound.number }}</span>
                                    <div class="flex items-center text-gray-300 text-xs">
                                        ----- <i class="fa-solid fa-plane mx-1"></i> -----
                                    </div>
                                    <span class="text-[10px] text-gray-400 mt-1">{{ flightInfo.inbound.airline }}</span>
                                </div>
                                <div class="flex flex-col items-end">
                                    <span class="text-xs text-gray-400">{{ flightInfo.inbound.arrival }}</span>
                                    <span class="font-bold text-lg text-gray-800">{{ flightInfo.inbound.arrivalTime }}</span>
                                    <span class="text-xs bg-blue-100 text-blue-600 px-1.5 py-0.5 rounded w-fit mt-1">
                                        {{ formatDateOnly(flightInfo.inbound.date) }}
                                    </span>
                                </div>
                            </div>

                        </div>
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

                <div v-if="currentTab === 'itinerary'">
                    <div class="flex justify-between mb-4">
                        <h2 class="text-xl font-bold">每日行程</h2>
                        <button @click="openAddModal" class="text-primary font-bold hover:text-lake-dark transition">
                            <i class="fa-solid fa-plus-circle"></i> 新增
                        </button>
                    </div>

                    <div v-if="itineraries.length === 0" class="text-center py-10 opacity-50">本日尚無行程</div>

                    <div v-for="(item, index) in sortedItinerary" :key="index" class="mb-4">

                        <div v-if="item.type === 'flight'"
                             class="relative bg-orange-50 p-4 rounded-2xl shadow-sm border-l-4 border-orange-400 flex justify-between items-start transition hover:shadow-md">

                            <div>
                                <div class="font-bold text-orange-600 text-lg">{{ item.data.departureTime }}</div>

                                <h3 class="text-lg font-medium text-gray-800 flex items-center gap-2">
                                    <i class="fa-solid fa-plane-departure text-orange-400"></i>
                                    {{ item.mode === 'outbound' ? '搭機前往' : '搭機返回' }} {{ item.data.arrival }}
                                </h3>

                                <p class="text-xs text-gray-500 mt-2 flex items-center gap-2">
                                    <span class="bg-white border border-orange-200 px-2 py-0.5 rounded text-orange-600 font-bold">
                                        {{ item.data.airline }}
                                    </span>
                                    <span>{{ item.data.number }}</span>
                                </p>

                                <div class="mt-2 text-xs text-gray-500 flex items-center gap-1">
                                    <span>{{ item.data.departure }} {{ item.data.departureTime }}</span>
                                    <i class="fa-solid fa-arrow-right-long text-orange-300 mx-1"></i>
                                    <span>{{ item.data.arrival }} {{ item.data.arrivalTime }}</span>
                                </div>
                            </div>

                            <div class="text-xs bg-orange-100 text-orange-600 px-3 py-1 rounded-full font-bold tracking-wider">
                                FLIGHT
                            </div>
                        </div>

                        <div v-else class="relative bg-white p-4 rounded-2xl shadow-sm border-l-4 border-primary group hover:shadow-md transition">
                            <div class="flex justify-between items-start">
                                <div class="cursor-pointer" @click="showOnMap(item.location)">
                                    <div class="font-bold text-lake-dark">{{ (item.time || '00:00').substring(0, 5) }}</div>
                                    <h3 class="text-lg font-medium text-gray-800 flex items-center">
                                        {{ item.location }}
                                        <i class="fa-solid fa-location-dot text-gray-300 ml-2 text-xs"></i>
                                    </h3>
                                    <p class="text-xs text-gray-400">{{ item.note }}</p>
                                </div>

                                <div class="flex flex-col items-end gap-2">
                                    <div class="text-blue-400 text-xs bg-blue-50 px-2 py-1 rounded h-fit">
                                        <i class="fa-solid fa-cloud-sun"></i> {{ item.temperature || '20' }}°C
                                    </div>
                                    <a :href="`http://googleusercontent.com/maps.google.com/maps?q=${item.location}`"
                                       target="_blank"
                                       class="text-xs bg-primary text-white px-3 py-1 rounded-full shadow hover:bg-lake-dark">
                                        <i class="fa-solid fa-location-arrow"></i> GO
                                    </a>
                                </div>
                            </div>

                            <div class="absolute bottom-2 right-2 flex space-x-3 opacity-0 group-hover:opacity-100 transition">
                                <button @click.stop="openEditModal(item)" class="text-gray-400 hover:text-primary" title="編輯">
                                    <i class="fa-solid fa-pen"></i>
                                </button>
                                <button @click.stop="deleteItem(item.id)" class="text-gray-400 hover:text-red-500" title="刪除">
                                    <i class="fa-solid fa-trash"></i>
                                </button>
                            </div>
                        </div>

                    </div>

                    
                </div>

                <div v-if="currentTab === 'map'" class="h-full flex flex-col">
                    <h2 class="text-xl font-bold mb-4">地圖導航</h2>
                    <div class="flex-1 bg-white rounded-2xl overflow-hidden shadow-sm relative border-2 border-white">
                        <iframe width="100%" height="100%" frameborder="0" style="border:0" :src="mapUrl" allowfullscreen></iframe>
                        <div class="absolute bottom-4 left-4 right-4 bg-white/90 backdrop-blur-sm p-3 rounded-xl shadow-lg text-sm">
                            <div class="font-bold text-lake-dark mb-1"><i class="fa-solid fa-map-pin mr-1"></i> 目前位置</div>
                            <div class="text-gray-700 text-lg font-medium mb-2">{{ mapLocation }}</div>
                            <a :href="`https://www.google.com/maps/dir/?api=1&destination=$?q=${mapLocation}`"
                               target="_blank"
                               class="block w-full text-center bg-primary text-white py-2 rounded-lg font-bold shadow hover:bg-lake-dark">
                                <i class="fa-solid fa-location-arrow mr-1"></i> 開啟導航
                            </a>
                        </div>
                    </div>
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

            <div v-if="showAddModal" class="absolute inset-0 bg-black/30 flex items-center justify-center z-50 p-6 backdrop-blur-sm">
                <div class="bg-white w-full rounded-2xl p-6 shadow-2xl max-h-[90vh] overflow-y-auto hide-scrollbar">

                    <h3 class="font-bold mb-4 text-lake-dark text-lg">
                        {{ isEditing ? '編輯行程' : '新增行程' }}
                    </h3>

                    <label class="text-xs text-gray-400 font-bold ml-1">參加人員</label>
                    <div class="flex flex-wrap gap-2 mb-3 bg-gray-50 p-2 rounded-xl border">
                        <label v-for="member in allMembers" :key="member"
                               class="flex items-center space-x-1 cursor-pointer select-none px-2 py-1 rounded transition"
                               :class="selectedParticipants.includes(member) ? 'bg-blue-100 text-blue-600' : 'text-gray-400'">

                            <input type="checkbox" :value="member" v-model="selectedParticipants" class="hidden">
                            <span class="text-sm font-bold">{{ member }}</span>
                            <i v-if="selectedParticipants.includes(member)" class="fa-solid fa-check text-xs"></i>

                        </label>
                    </div>

                    <label class="text-xs text-gray-400 font-bold ml-1">時間</label>
                    <input v-model="newItem.time" type="time" class="w-full bg-gray-50 p-3 rounded-xl mb-3 border focus:border-primary outline-none transition">

                    <label class="text-xs text-gray-400 font-bold ml-1">地點名稱</label>
                    <input v-model="newItem.location" placeholder="例如: 福岡塔" class="w-full bg-gray-50 p-3 rounded-xl mb-3 border focus:border-primary outline-none transition">

                    <div v-if="newItem.location" class="mb-4 rounded-xl overflow-hidden border border-gray-200 h-40 bg-gray-100">
                        <iframe width="100%" height="100%" frameborder="0" style="border:0" :src="newLocationPreviewUrl" allowfullscreen></iframe>
                    </div>

                    <label class="text-xs text-gray-400 font-bold ml-1">備註</label>
                    <input v-model="newItem.note" placeholder="例如: 記得帶相機" class="w-full bg-gray-50 p-3 rounded-xl mb-6 border focus:border-primary outline-none transition">

                    <div class="flex gap-3">
                        <button @click="showAddModal=false" class="flex-1 bg-gray-100 text-gray-500 py-3 rounded-xl font-bold hover:bg-gray-200 transition">取消</button>
                        <button @click="saveItem" class="flex-1 bg-primary text-white py-3 rounded-xl font-bold shadow-lg shadow-primary/30 hover:bg-lake-dark transition">
                            {{ isEditing ? '儲存修改' : '確認新增' }}
                        </button>
                    </div>
                </div>
            </div>

        </div>
    </div>
</template>
