import { createRouter, createWebHashHistory } from 'vue-router'
import RequestsView from '../views/RequestsView.vue'
import RulesView from '../views/RulesView.vue'
import ProxyView from '../views/ProxyView.vue'
import SettingsView from '../views/SettingsView.vue'
import TopologyView from '../views/TopologyView.vue'
import PerformanceView from '../views/PerformanceView.vue'
import ComparisonView from '../views/ComparisonView.vue'

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
    path: '/topology',
    name: 'Topology',
    component: TopologyView
  },
  {
    path: '/performance',
    name: 'Performance',
    component: PerformanceView
  },
  {
    path: '/settings',
    name: 'Settings',
    component: SettingsView
  },
  {
    path: '/comparison',
    name: 'Comparison',
    component: ComparisonView
  }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes
})

export default router
