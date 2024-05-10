import { RouteRecordRaw } from "vue-router";

const routes: RouteRecordRaw[] = [
    { path: "banner", component: () => import("./views/banner.vue"), meta: { rule: "Banner" } },
    { path: "news", component: () => import("./views/news.vue"), meta: { rule: "News" } },
    { path: "url-segment", component: () => import("./views/url-segment.vue") },
    { path: "page", component: () => import("./views/page.vue") },
  ];

export default routes;