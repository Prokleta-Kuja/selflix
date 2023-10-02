import { createRouter, createWebHistory, type RouteLocationNormalized } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import { useAuth } from '@/stores/auth'

const parseId = (route: RouteLocationNormalized) => {
  let parsed = parseInt(route.params.id.toString())
  if (isNaN(parsed)) parsed = 0

  return { ...route.params, id: parsed }
}

const router = createRouter({
  linkActiveClass: 'active',
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView
    },
    {
      path: '/sign-in',
      name: 'sign-in',
      component: () => import('../views/SignInView.vue')
    },
    {
      path: '/sign-out',
      name: 'sign-out',
      component: () => import('../views/SignInView.vue')
    },
    {
      path: '/users',
      name: 'users',
      component: () => import('../views/UserView.vue')
    },
    {
      path: '/users/:id(\\d+)',
      name: 'user',
      component: () => import('../views/UserView.vue')
    },
    {
      path: '/libs',
      name: 'libs',
      component: () => import('../views/SignInView.vue')
    },
    {
      path: '/libs/:id(\\d+)',
      name: 'lib',
      props: parseId,
      component: () => import('../views/SignInView.vue')
    },
    { path: '/:pathMatch(.*)*', name: 'NotFound', component: HomeView } // NotFound
  ]
})

const publicPages = ['/sign-in', '/sign-out']
router.beforeEach(async (to) => {
  const auth = useAuth()
  const authRequired = !publicPages.includes(to.path)

  // Must wait for auth to intialize before making a decision
  while (!auth.initialized) await new Promise((f) => setTimeout(f, 500))

  if (!auth.isAuthenticated && authRequired) return { name: 'sign-in' }
})

export default router
