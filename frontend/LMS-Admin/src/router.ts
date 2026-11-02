import { createRouter, createWebHistory } from 'vue-router'
import PortalView from './views/PortalView.vue'
import LoginView from './views/LoginView.vue'
import { isAuthenticated, isDemoMode } from './api/session'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/portal/locations' },
    { path: '/login', component: LoginView },
    { path: '/portal/:section', component: PortalView },
    { path: '/:pathMatch(.*)*', redirect: '/portal/locations' },
  ],
})

router.beforeEach((to) => {
  if (isDemoMode) return true
  if (to.path === '/login') return isAuthenticated.value ? '/portal/locations' : true
  if (!isAuthenticated.value) return { path: '/login', query: { redirect: to.fullPath } }
  return true
})

export default router
