// @ts-nocheck

import context, { IConfiguration, IJsonResponse, ModelState } from "@cloudfun/core";
import { Module } from "vuex";

const MAX_ALIVE_TIME = 15000;
const apiPath = "/Configuration";

interface State {
    loadingTime?: Date;
    value?: IConfiguration;
}

const module: Module<State, ModelState> = {
    namespaced: true,
    state: {
        value: { guest: context.guest }
    },
    getters: {
        isExpired: state => !state.loadingTime || (new Date().valueOf() - state.loadingTime.valueOf() < MAX_ALIVE_TIME),
    },
    mutations: {
        setValue(state, value) {
            Object.assign(state.value, value);
        }
    },
    actions: {
        read: ({ rootState, state, getters, commit }) => new Promise((resolve, reject) => {
            if (!getters.isExpired) resolve(state.value);
            else {
                rootState.clients.unauthorized.get(apiPath).then(
                    (response: IJsonResponse<IConfiguration>) => {
                        state.loadingTime = new Date();
                        document.title = response.payload.AdminTitle;
                        commit('setValue', response.payload);
                        resolve(state.value);
                    },
                    failure => reject(failure)
                );
            }
        }),
        update: ({ rootState, state, commit }, value: IConfiguration) => new Promise((resolve, reject) => {
            rootState.clients.authorized.put<IConfiguration>(apiPath, value).then(
                (success: IJsonResponse<IConfiguration>) => {
                    state.loadingTime = new Date();
                    commit('setValue', success.payload);
                    resolve(state.value);
                },
                failure => reject(failure)
            );
        }),
    }
}

export default module;