import { createRouter, createWebHashHistory } from 'vue-router'
import RequestsView from '../views/RequestsView.vue'
import RulesView from '../views/RulesView.vue'
import ProxyView from '../views/ProxyView.vue'
import SettingsView from '../views/SettingsView.vue'

const routes = [
  {
    path: '/',
    redirect: '/requests'
  },
  {
    path: '/requests',
    name: 'Requests',
    component: RequestsView
  },
  {
    path: '/rules',
    name: 'Rules',
    component: RulesView
  },
  {
    path: '/proxy',
    name: 'Proxy',
    component: ProxyView
  },
  {
    path: '/settings',
    name: 'Settings',
    component: SettingsView
  }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes
})

export default router
