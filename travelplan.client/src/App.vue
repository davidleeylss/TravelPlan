<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import axios from 'axios';

const currentTab = ref('itinerary');
const currentDate = ref('2025-04-10');
const dates = ['2025-04-10', '2025-04-11', '2025-04-12', '2025-04-13'];
const showAddModal = ref(false);

// 判斷目前是否為編輯模式
const isEditing = ref(false);

// --- 地圖相關變數 ---
const mapLocation = ref('博多駅'); // 預設地圖地點
const mapUrl = computed(() => {
    // 使用 Google Maps 舊版 Embed 格式 (免 API Key 快速測試用)
    // 如果你有 API Key，建議改用: `https://www.google.com/maps/embed/v1/place?key=你的KEY&q=${mapLocation.value}`
    return `https://maps.google.com/maps?q=${encodeURIComponent(mapLocation.value)}&z=15&output=embed`;
});

// --- 切換到地圖並顯示地點 ---
const showOnMap = (location) => {
    mapLocation.value = location; // 更新地圖地點
    currentTab.value = 'map';     // 切換分頁
};

// 【新增】用來即時預覽「新增視窗」裡的地圖
const newLocationPreviewUrl = computed(() => {
    if (!newItem.value.location) return '';
    // 當使用者輸入地點時，自動產生 Google Maps Embed 連結
    return `https://www.google.com/maps?q=${encodeURIComponent(newItem.value.location)}&output=embed`;
});

// 資料狀態
const itineraries = ref([]);
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
        const res = await axios.get(`/api/itinerary?date=${currentDate.value}`);
        itineraries.value = res.data;
    } catch (err) {
        console.error(err);
    }
};

// API: 新增行程
const saveItem = async () => {
    if (!newItem.value.location) return;
    
    try {
        if (isEditing.value) {
            // --- 編輯模式 ---
            await axios.put(`/api/itinerary/${newItem.value.id}`, {
                id: newItem.value.id,
                date: formatDate(currentDate.value),
                time: newItem.value.time,
                location: newItem.value.location,
                note: newItem.value.note
            });
        } else {
            // --- 新增模式 ---
            await axios.post('/api/itinerary', {
                date: formatDate(currentDate.value),
                time: newItem.value.time,
                location: newItem.value.location,
                note: newItem.value.note
            });
        }
        
        await fetchItineraries(); // 重整列表
        showAddModal.value = false;
        resetForm(); // 清空表單
    } catch (err) {
        console.error(err);
        alert('儲存失敗，請檢查後端是否已重啟');
    }
};

// 開啟新增視窗 (重置表單)
const openAddModal = () => {
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
    if(newItem.value.time.length > 5) {
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
    if(!confirm("確定刪除?")) return;
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
const getDayName = (d) => ['SUN','MON','TUE','WED','THU','FRI','SAT'][new Date(d).getDay()];
const getDayNum = (d) => d.split('-')[2];

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
                <div class="flex justify-between items-center mb-4">
                    <h1 class="text-2xl font-bold tracking-widest text-lake-dark">FUKUOKA</h1>
                    <div class="text-xs text-gray-400">福岡之旅</div>
                </div>
                <div class="flex space-x-3 overflow-x-auto hide-scrollbar pb-2">
                    <div v-for="day in dates" :key="day" @click="currentDate = day"
                         :class="['flex-shrink-0 w-14 h-20 rounded-2xl flex flex-col justify-center items-center transition border cursor-pointer',
                                  currentDate === day ? 'bg-lake-green text-white shadow-lg -translate-y-1' : 'bg-white text-gray-400']">
                        <span class="text-xs">{{ getDayName(day) }}</span>
                        <span class="text-xl font-bold">{{ getDayNum(day) }}</span>
                    </div>
                </div>
            </header>

            <main class="flex-1 overflow-y-auto hide-scrollbar p-5 pb-24">

                <div v-if="currentTab === 'itinerary'">
                    <div class="flex justify-between mb-4">
                        <h2 class="text-xl font-bold">每日行程</h2>
                        <button @click="openAddModal" class="text-lake-green font-bold hover:text-lake-dark transition">
                            <i class="fa-solid fa-plus-circle"></i> 新增
                        </button>
                    </div>

                    <div v-if="itineraries.length === 0" class="text-center py-10 opacity-50">本日尚無行程</div>

                    <div v-for="item in itineraries" :key="item.id" class="relative bg-white p-4 rounded-2xl shadow-sm border-l-4 border-lake-green mb-4 group hover:shadow-md transition">
                        <div class="flex justify-between items-start">
                            <div class="cursor-pointer" @click="showOnMap(item.location)">
                                <div class="font-bold text-lake-dark">{{ (item.time || item.Time || '00:00').substring(0, 5) }}</div>
                                <h3 class="text-lg font-medium text-gray-800 flex items-center">
                                    {{ item.location || item.Location }}
                                    <i class="fa-solid fa-location-dot text-gray-300 ml-2 text-xs"></i>
                                </h3>
                                <p class="text-xs text-gray-400">{{ item.note || item.Note }}</p>
                            </div>

                            <div class="flex flex-col items-end gap-2">
                                <div class="text-blue-400 text-xs bg-blue-50 px-2 py-1 rounded h-fit">
                                    <i class="fa-solid fa-cloud-sun"></i> {{ item.temperature || '20' }}°C
                                </div>
                                <a :href="`https://www.google.com/maps/dir/?api=1&destination=$?q=${item.location}`"
                                   target="_blank"
                                   class="text-xs bg-lake-green text-white px-3 py-1 rounded-full shadow hover:bg-lake-dark">
                                    <i class="fa-solid fa-location-arrow"></i> GO
                                </a>
                            </div>
                        </div>

                        <div class="absolute bottom-2 right-2 flex space-x-3 opacity-0 group-hover:opacity-100 transition">
                            <button @click.stop="openEditModal(item)" class="text-gray-400 hover:text-lake-green" title="編輯">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button @click.stop="deleteItem(item.id)" class="text-gray-400 hover:text-red-500" title="刪除">
                                <i class="fa-solid fa-trash"></i>
                            </button>
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
                               class="block w-full text-center bg-lake-green text-white py-2 rounded-lg font-bold shadow hover:bg-lake-dark">
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
                        <button @click="addExpense" class="w-full bg-lake-green text-white py-2 rounded font-bold">記帳</button>
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
                <button @click="currentTab='itinerary'" :class="['flex flex-col items-center space-y-1 transition', currentTab==='itinerary'?'text-lake-green':'text-gray-300']">
                    <i class="fa-solid fa-list-ul text-xl"></i>
                </button>
                <button @click="currentTab='map'" :class="['flex flex-col items-center space-y-1 transition', currentTab==='map'?'text-lake-green':'text-gray-300']">
                    <i class="fa-solid fa-map-location-dot text-xl"></i>
                </button>
                <button @click="currentTab='expenses'" :class="['flex flex-col items-center space-y-1 transition', currentTab==='expenses'?'text-lake-green':'text-gray-300']">
                    <i class="fa-solid fa-calculator text-xl"></i>
                </button>
            </nav>

            <div v-if="showAddModal" class="absolute inset-0 bg-black/30 flex items-center justify-center z-50 p-6 backdrop-blur-sm">
                <div class="bg-white w-full rounded-2xl p-6 shadow-2xl max-h-[90vh] overflow-y-auto hide-scrollbar">

                    <h3 class="font-bold mb-4 text-lake-dark text-lg">
                        {{ isEditing ? '編輯行程' : '新增行程' }}
                    </h3>

                    <label class="text-xs text-gray-400 font-bold ml-1">時間</label>
                    <input v-model="newItem.time" type="time" class="w-full bg-gray-50 p-3 rounded-xl mb-3 border focus:border-lake-green outline-none transition">

                    <label class="text-xs text-gray-400 font-bold ml-1">地點名稱</label>
                    <input v-model="newItem.location" placeholder="例如: 福岡塔" class="w-full bg-gray-50 p-3 rounded-xl mb-3 border focus:border-lake-green outline-none transition">

                    <div v-if="newItem.location" class="mb-4 rounded-xl overflow-hidden border border-gray-200 h-40 bg-gray-100">
                        <iframe width="100%" height="100%" frameborder="0" style="border:0" :src="newLocationPreviewUrl" allowfullscreen></iframe>
                    </div>

                    <label class="text-xs text-gray-400 font-bold ml-1">備註</label>
                    <input v-model="newItem.note" placeholder="例如: 記得帶相機" class="w-full bg-gray-50 p-3 rounded-xl mb-6 border focus:border-lake-green outline-none transition">

                    <div class="flex gap-3">
                        <button @click="showAddModal=false" class="flex-1 bg-gray-100 text-gray-500 py-3 rounded-xl font-bold hover:bg-gray-200 transition">取消</button>
                        <button @click="saveItem" class="flex-1 bg-lake-green text-white py-3 rounded-xl font-bold shadow-lg shadow-lake-green/30 hover:bg-lake-dark transition">
                            {{ isEditing ? '儲存修改' : '確認新增' }}
                        </button>
                    </div>
                </div>
            </div>

        </div>
    </div>
</template>
