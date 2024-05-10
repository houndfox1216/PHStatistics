import { RouteRecordRaw } from "vue-router";

const routes: RouteRecordRaw[] = [
    { path: "media-file", component: () => import("./views/media-file.vue"), meta: { rule: "MediaFile" } },
    { path: "live-source", component: () => import("./views/live-source.vue"), meta: { rule: "LiveSource" } },
  ];

export default routes;