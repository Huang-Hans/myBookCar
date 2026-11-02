import { ref } from 'vue'

export type Locale = 'zh-Hant' | 'en'

const storageKey = 'lmsUserAppLocale'

function savedLocale(): Locale {
  try { return typeof uni !== 'undefined' && uni.getStorageSync(storageKey) === 'en' ? 'en' : 'zh-Hant' }
  catch { return 'zh-Hant' }
}

export const locale = ref<Locale>(savedLocale())

export function setLocale(value: Locale): void {
  locale.value = value
  try { if (typeof uni !== 'undefined') uni.setStorageSync(storageKey, value) }
  catch { /* The current session still uses the selected language. */ }
}

const english: Record<string, string> = {
  '工作台': 'Home', '預約': 'Bookings', '行程': 'Trips', '派車': 'Dispatch', '用戶': 'User',
  '消息通知': 'Notifications', '員工出行服務總覽': 'Staff transport overview',
  '用戶中心': 'User account', '管理個人資料及偏好設定': 'Manage your profile and preferences',
  '員工資料': 'Staff profile', '控制台設定': 'Console settings', '說明與支援': 'Help & support', '登出': 'Sign out',
  '語言': 'Language', '英文': 'English', '繁體中文': 'Traditional Chinese',
  '角色': 'Role', '員工編號': 'Staff ID', '物業 / 崗位': 'Property / Station',
  '企業電郵': 'Corporate email', '前台': 'Front Desk', '禮賓部': 'Concierge',
  '預設通知渠道': 'Default notification channel', '客房訊息': 'In-room message',
  '儲存設定': 'Save preferences', '儲存中…': 'Saving…', '設定已儲存': 'Preferences saved',
  '返回': 'Back', '所有預約及更改均會記錄您的員工身份；主管審批須另行授權。': 'All bookings and changes are tagged with your staff identity. Manager decisions require separate authorization.',
  '如需接送協助，請聯絡禮賓營運部，分機 8888。': 'For transfer assistance, contact Concierge Operations at ext. 8888.',
  '審批事宜請聯絡當值前台經理。': 'For approval requests, contact the on-duty Front Desk Manager.',
  '支援服務全天候提供。': 'Support is available 24 hours a day.',
  '偏好設定會儲存到您的 LMS 帳戶。': 'Preferences are saved to your LMS account.',
  '近期概況': 'Overview', '進行中行程': 'Active trips', '待派車': 'Awaiting dispatch', '已完成': 'Completed',
  '快捷操作': 'Quick actions', '快速預約': 'Quick booking', '派車服務': 'Dispatch service',
  '為賓客建立行程': 'Create a guest trip', '處理待派車訂單': 'Handle pending dispatch orders',
  '最新行程': 'Latest trips', '查看全部 ›': 'View all ›', '登出帳戶': 'Sign out',
  '基於最近 50 個行程及 20 個派車單': 'Based on the latest 50 trips and 20 dispatch orders',
  '暫無行程，從快速預約開始。': 'No trips yet. Start with quick booking.',
  '重新整理': 'Refresh', '載入中…': 'Loading…', '重試': 'Retry',
  '預約列表': 'Booking list', '查找及管理所有賓客預約': 'Find and manage guest bookings',
  '搜尋': 'Search', '搜尋訂單或賓客': 'Search orders or guests', '全部': 'All',
  '進行中': 'Active', '已取消': 'Cancelled', '預約記錄': 'Booking records',
  '＋ 新增': '+ New', '沒有符合條件的預約': 'No matching bookings',
  '上一頁': 'Previous', '下一頁': 'Next',
  '行程追蹤': 'Trip tracking', '實時掌握進行中的行程': 'Track active trips in real time',
  '進行中 ·': 'Active ·', '行程狀態由司機及派車系統更新；點擊行程查看完整時間線。': 'Trip status is updated by the driver and dispatch system. Tap a trip for its full timeline.',
  '目前沒有進行中的行程': 'No active trips', '已預約': 'Booked', '司機出發': 'Driver en route', '行程中': 'In progress',
  '派車訂單': 'Dispatch orders', '管理司機邀請及派車狀態': 'Manage driver invitations and dispatch status',
  '派車操作': 'Dispatch options',
  '等待派車': 'Awaiting dispatch', '選擇訂單，邀請合適的司機': 'Select an order and invite an available driver',
  '目前沒有派車訂單': 'No dispatch orders', '選擇司機 ›': 'Select driver ›',
  '可邀請司機': 'Available drivers', '目前不可派車': 'Dispatch unavailable',
  '目前沒有可用司機': 'No available drivers', '車輛待確認': 'Vehicle to be confirmed', '當值': 'On duty',
  '狀態未知': 'Status unknown', '邀請 ›': 'Invite ›', '確認派車': 'Confirm dispatch',
  '司機：': 'Driver:', '等待': 'Waiting for', '回覆': 'to respond',
  '已發送司機邀請': 'Driver invitation sent', '關閉': 'Close',
  '通知中心': 'Notification center', '重要行程消息不遺漏': 'Stay on top of important trip updates',
  '全部標為已讀': 'Mark all as read', '未讀': 'Unread', '目前沒有通知': 'No notifications',
  '通知篩選': 'Notification filter',
  '行程詳情': 'Trip details', '預約及行程進度': 'Booking and trip progress',
  '找不到行程': 'Trip not found', '預約資料': 'Booking details', '賓客': 'Guest',
  '車型': 'Vehicle class', '物業': 'Property', '出發時間': 'Departure time', '訂單狀態': 'Order status', '乘客 / 行李': 'Passengers / luggage',
  '行程時間線': 'Trip timeline', '尚無行程事件': 'No trip events yet',
  '行程地圖': 'Trip map', '軌跡已抽樣顯示': 'Track has been sampled',
  'A 上車點': 'A Pickup', 'B 下車點': 'B Drop-off', '司機實時位置': 'Live driver location',
  '等待定位更新': 'Waiting for location updates', '暫無行駛軌跡': 'No driving track yet',
  '暫無司機定位': 'No driver location yet', '高德路線預估': 'AMap route estimate',
  '暫無道路路線，顯示直線估算': 'Road route unavailable; showing a straight-line estimate',
  '放大地圖': 'Zoom in', '縮小地圖': 'Zoom out', '行程地址': 'Trip addresses',
  '上車點': 'Pickup', '下車點': 'Drop-off', '未知上車地點': 'Pickup location unavailable', '未知下車地點': 'Drop-off location unavailable', '司機位置': 'Driver location',
  '已行駛公里': 'km covered', '距接載地點公里': 'km to pickup',
  '公里 / 小時': 'km/h', '預計行車時間': 'ETA', '實時定位': 'Live GPS',
  '定位更新': 'Location updated', 'AMap is not configured.': 'AMap is not configured.',
  'Unable to load Amap.': 'Unable to load AMap.', 'Unable to place the trip on the map.': 'Unable to place the trip on the map.',
  '待確認': 'Pending confirmation', '未派車': 'Not dispatched', '已指派': 'Assigned',
  '司機前往中': 'Driver en route', '司機已抵達': 'Driver arrived',
  '等待司機回覆': 'Awaiting driver response', '需重新派車': 'Needs redispatch',
  '員工出行服務': 'Staff transport services', '預約、行程與派車，一站處理。': 'Bookings, trips and dispatch in one place.',
  '歡迎回來': 'Welcome back', '請使用企業帳戶登入': 'Sign in with your corporate account',
  '用戶名稱': 'Username', '密碼': 'Password', '輸入用戶名稱': 'Enter username',
  '輸入密碼': 'Enter password', '登入中…': 'Signing in…', '登入工作台': 'Sign in',
  '請輸入用戶名稱和密碼': 'Enter your username and password',
  '為賓客安排專屬行程': 'Arrange a guest trip', '01 · 賓客資料': '01 · Guest details',
  '02 · 行程安排': '02 · Trip details', '03 · 車輛與乘客': '03 · Vehicle and passengers',
  '搜尋賓客': 'Search guests', '姓名或會員編號': 'Name or membership ID',
  '所屬物業': 'Property', '選擇物業': 'Select property', '服務類型': 'Service type',
  '暫無可選物業': 'No properties available', '暫無可選地點': 'No locations available',
  '搜尋選項': 'Search options', '沒有符合條件的選項': 'No matching options',
  '單程': 'One way', '機場接機': 'Airport pickup', '機場送機': 'Airport drop-off', '酒店往返': 'Between properties',
  '上車地點': 'Pickup', '選擇上車地點': 'Select pickup', '目的地': 'Destination',
  '選擇目的地': 'Select destination', '日期': 'Date', '選擇日期': 'Select date',
  '時間': 'Time', '選擇時間': 'Select time', '豪華轎車': 'Luxury sedan',
  'Luxury Sedan': '豪華轎車', 'Luxury MPV': '豪華 MPV', 'Standard Sedan': '普通轎車', 'Limousine': '禮賓車', 'Maybach': '邁巴赫', 'Party': '聚會用車', 'Cross-Border Transfer': '跨境接送',
  '出發日期與時間': 'Departure date and time', '選擇出發日期與時間': 'Choose departure date and time',
  '小時': 'Hour', '分鐘': 'Minute', '澳門時間': 'Macau time', '確認': 'Confirm',
  '豪華 MPV': 'Luxury MPV', '乘客': 'Passengers', '行李': 'Luggage',
  '暫無可選車型': 'No vehicle options available', '請選擇可用車型': 'Select an available vehicle option',
  '預約預覽': 'Booking preview', '此預約可能進入候補': 'This booking may be waitlisted',
  '預約確認': 'Confirm booking details', '確認預約': 'Confirm booking', '返回修改': 'Back to edit',
  '此預約需要審批': 'This booking requires approval', '所需積分：': 'Points required:',
  '檢查中…': 'Checking…', '檢查預約': 'Check booking', '提交中…': 'Submitting…',
  '確認並建立預約': 'Confirm and create booking', '預約已建立': 'Booking created',
  '請輸入賓客姓名或會員編號': 'Enter a guest name or membership ID',
  '請完成賓客、物業、路線及出發時間': 'Complete the guest, property, route and departure time',
  '上車地點與目的地不可相同': 'Pickup and destination must differ',
  '出發時間須在未來': 'Departure time must be in the future',
  '請輸入有效的乘客及行李數量': 'Enter valid passenger and luggage counts',
  '請輸入有效的乘客數量': 'Enter a valid passenger count',
  '崗位': 'Station',
  '邀請記錄': 'Invitation history',
}

export function t(value: string): string { return locale.value === 'en' ? english[value] ?? value : value }
