import { Model, ModelState } from "@cloudfun/core";
import { Module } from "vuex";

import batchUploader from "./batch-uploader";
import basicModule from "@/modules/basis/models"
import contentModule from "@/modules/content/models";
import manufactureModule from "@/modules/manufacture/models";
import streamingModule from "@/modules/streaming/models";

// Main module
const midone: Module<{ layoutModeValue: string, colorSchemeValue: string, darkModeValue?: boolean, desktopModeValue?: boolean, lastLoginValue: string, activeMenuValue: boolean }, ModelState> = {
    namespaced: true,
    state: {
        layoutModeValue: localStorage.getItem("layoutMode") ?? "side",
        colorSchemeValue: localStorage.getItem("colorScheme") ?? "default",
        darkModeValue: localStorage.getItem("darkMode") === null ? undefined : localStorage.getItem("darkMode") === "true",
        desktopModeValue: localStorage.getItem("desktopMode") === null ? undefined : localStorage.getItem("desktopMode") === "true",
        lastLoginValue: localStorage.getItem("lastLogin") ?? "",
        activeMenuValue: localStorage.getItem("activeMenu") == null ? true : localStorage.getItem("activeMenu") == "true",
    },
    getters: {
        layoutMode: (state) => ["side", "simple", "top"].includes(state.layoutModeValue) ? state.layoutModeValue : "side",
        colorScheme: (state) => state.colorSchemeValue,
        darkMode: (state, getters) => {
            const isDarkColorScheme = window.matchMedia('(prefers-color-scheme: dark)').matches;
            if (!getters.desktopMode) return isDarkColorScheme;
            return state.darkModeValue !== undefined ? state.darkModeValue : isDarkColorScheme;
        },
        desktopMode: (state) => state.desktopModeValue !== undefined ? state.desktopModeValue : true,
        lastLogin: (state) => state.lastLoginValue,
        activeMenu: (state) => state.activeMenuValue,
    },
    mutations: {
        setLayoutMode(state, value) {
            localStorage.setItem("layoutMode", value);
            state.layoutModeValue = value;
        },
        setColorScheme(state, colorScheme) {
            localStorage.setItem("colorScheme", colorScheme);
            state.colorSchemeValue = colorScheme;
        },
        setDarkMode(state, value) {
            if (value == undefined) localStorage.removeItem("darkMode");
            else localStorage.setItem("darkMode", value);
            state.darkModeValue = value;
        },
        setDesktopMode(state, value) {
            if (value == undefined) localStorage.removeItem("desktopMode");
            else localStorage.setItem("desktopMode", value);
            state.desktopModeValue = value;
        },
        setLastLogin(state, lastLogin) {
            localStorage.setItem("lastLogin", lastLogin);
            state.lastLoginValue = lastLogin;
        },
        setActiveMenu(state, activeMenu) {
            localStorage.setItem("activeMenu", activeMenu);
            state.activeMenuValue = activeMenu;
        },
    },
}

// Build model form all VUEX Modules;
const model = new Model('empty-project', {
    midone,
    batchUploader,
    ...basicModule,
    ...contentModule,
    ...manufactureModule,
    ...streamingModule,
});

const serviceBaseUri = `${import.meta.env.VITE_SERVICE_URI}/api`;
model.clients.authorized = model.createHttpClient(serviceBaseUri, true);
model.clients.unauthorized = model.createHttpClient(serviceBaseUri);
model.onLogin = (data) => model.clients.unauthorized.post('System/Login', data);
model.onLogout = () => model.clients.authorized.post('System/Logout');
model.onReloadUser = () => model.clients.authorized.post('System/CurrentUser');
model.onReloadEnums = () => model.clients.unauthorized.get('System/Enumerations');
model.onLog = (message) => model.clients.unauthorized.post('System/Log', message);

export default model;
