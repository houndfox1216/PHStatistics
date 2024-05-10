import { createRouter, createWebHashHistory, RouteRecordRaw } from "@cloudfun/core";

import basisModule from "@/modules/basis/router";
import contentModule from "@/modules/content/router";
import manufactureModule from "@/modules/manufacture/router";
import streamingModule from "@/modules/streaming/router";
import midoneTemplate from "@/midone-template/router";

// View routes
const viewRoutes: RouteRecordRaw[] = [
  { path: "dashboard", component: () => import("@/views/dashboard.vue") },
  {
    path: "profile",
    component: () => import("@/views/profile/layout.vue"),
    children: [
      { path: "", component: () => import("@/views/profile/information.vue") },
      { path: "change-password", component: () => import("@/views/profile/change-password.vue") },
    ]
  },
  {
    path: "components",
    component: () => import("@/views/components/layout.vue"),
    children: [
      { path: "checkbox-list", component: () => import("@/views/components/checkbox-list.vue") },
      { path: "stepper", component: () => import("@/views/components/stepper.vue") },
      { path: "placeholder", component: () => import("@/views/components/placeholder.vue") },
      { path: "file-uploader", component: () => import("@/views/components/file-uploader.vue") },
    ]
  },
  { path: "information", component: () => import("./views/information.vue") },
  ...basisModule,
  ...contentModule,
  ...manufactureModule,
  ...midoneTemplate,
  ...streamingModule,
];

// layout routes
const routes = [ 
  {
    path: "/",
    component: () => import("@/layouts/Main.vue"),
    redirect: "/dashboard",
    children: viewRoutes,
  },
  {
    path: "/login",
    component: () => import("@/views/login.vue"),
  },
  {
    path: "/register",
    component: () => import("@/views/register.vue"),
  },
  {
    path: "/:pathMatch(.*)*",
    component: () => import("@/views/error-page.vue"),
  },
];

const router = createRouter({
  history: createWebHashHistory(),
  routes,
  scrollBehavior(_to, _from, savedPosition) {
    return savedPosition || { left: 0, top: 0 };
  },
});

export default router;
