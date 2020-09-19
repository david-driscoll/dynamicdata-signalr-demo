import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';
import Heros from '../views/Heros.vue';

const routes: Array<RouteRecordRaw> = [
    {
        path: '/',
        name: 'Heros',
        component: Heros,
    },
    {
        path: '/hero-tree',
        name: 'Hero Tree',
        // route level code-splitting
        // this generates a separate chunk (about.[hash].js) for this route
        // which is lazy-loaded when the route is visited.
        component: () => import(/* webpackChunkName: "about" */ '../views/HeroTree.vue'),
    },
];

const router = createRouter({
    history: createWebHistory(process.env.BASE_URL),
    routes,
});

export default router;
