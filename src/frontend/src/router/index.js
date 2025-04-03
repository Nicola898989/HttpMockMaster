import { createRouter, createWebHistory } from 'vue-router';
import Home from '../views/Home.vue';
import Requests from '../views/Requests.vue';
import Rules from '../views/Rules.vue';
import Proxy from '../views/Proxy.vue';

const routes = [
  {
    path: '/',
    name: 'Home',
    component: Home
  },
  {
    path: '/requests',
    name: 'Requests',
    component: Requests
  },
  {
    path: '/rules',
    name: 'Rules',
    component: Rules
  },
  {
    path: '/proxy',
    name: 'Proxy',
    component: Proxy
  }
];

const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes
});

export default router;
