import { PolicyRule, RouteRecordRaw } from "@cloudfun/core";

const routes: RouteRecordRaw[] = [
    { path: "configuration", component: () => import("./views/configuration.vue"), meta: { rule: "Configuration" } },
    { path: "person", component: () => import("./views/person.vue"), meta: { rule: "Person" } },
    { path: "permission-wizard", component: () => import("./views/permission/wizard.vue"), meta: { rule: new PolicyRule("Role").and("User") } },
    { path: "role", component: () => import("./views/permission/role.vue"), meta: { rule: "Role" } },
    { path: "user", component: () => import("./views/permission/user.vue"), meta: { rule: "User" } },
    { path: "action-log", component: () => import("./views/action-log/index.vue"), meta: { rule: "ActionLog" } },
    { path: "attribute", component: () => import("./views/attribute.vue"), meta: { rule: "Attribute" } },
    { path: "album", component: () => import("./views/album.vue"), meta: { rule: "Album" } },
    { path: "category", component: () => import("./views/category.vue"), meta: { rule: "Category" } },
    { path: "tag", component: () => import("./views/tag.vue"), meta: { rule: "Tag" } },
];

export default routes;