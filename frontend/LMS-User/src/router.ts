import { createRouter, createWebHistory } from 'vue-router'
import ConsoleView from './views/ConsoleView.vue'
import LoginView from './views/LoginView.vue'
import { isAuthenticated, isDemoMode } from './api/session'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/console/tracking' },
    { path: '/login', component: LoginView },
    { path: '/console/locations', redirect: '/console/tracking' },
    { path: '/console/booking', redirect: '/console/manage' },
    { path: '/console/guests', redirect: '/console/members' },
    { path: '/console/:section', component: ConsoleView },
    { path: '/:pathMatch(.*)*', redirect: '/console/tracking' },
  ],
})

router.beforeEach((to) => {
  if (isDemoMode) return true
  if (to.path === '/login') return isAuthenticated.value ? '/console/tracking' : true
  if (!isAuthenticated.value) return { path: '/login', query: { redirect: to.fullPath } }
  return true
})

export default router
