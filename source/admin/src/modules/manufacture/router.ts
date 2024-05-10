import { RouteRecordRaw } from "vue-router";

const routes: RouteRecordRaw[] = [
    { path: "product", component: () => import("./views/product.vue"), meta: { rule: "Product" } },
  ];

export default routes;