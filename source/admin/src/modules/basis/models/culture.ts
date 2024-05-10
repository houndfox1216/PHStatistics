// @ts-nocheck

import { deepEqual } from 'fast-equals';
import { ModelState, IJsonResponse, IUserData } from "@cloudfun/core";
import { Module } from "vuex";

const MAX_ALIVE_TIME = 15000;
const apiPath = "/Culture";

interface State {
  loadingTime?: Date;
  params: any,
  totalCount: number,
  rows: any[];
}

const module: Module<State, ModelState> = {
  namespaced: true,
  state: {
    params: {},
    totalCount: 0,
    rows: [],
  },
  getters: {
    isExpired: state => !state.loadingTime || (new Date().valueOf() - state.loadingTime.valueOf() < MAX_ALIVE_TIME),
    page: state => state.params.page,
    pageSize: state => state.params.pageSize,
    condition: state => state.params.condition,
    sortings: state => state.params.sortings,
    totalCount: state => state.totalCount,
    rows: state => state.rows,
  },
  mutations: {
    setPage(state, page) {
      if (state.params.page == page) return;
      state.params.page = page;
      state.loadingTime = undefined;
    },
    setPageSize(state, pageSize) {
      if (state.params.pageSize == pageSize) return;
      state.params.pageSize = pageSize
      state.loadingTime = undefined;
    },
    setCondition(state, condition) {
      if (deepEqual(state.params.condition, condition)) return;
      state.params.condition = condition
      state.loadingTime = undefined;
    },
    setSortings(state, sortings) {
      if (deepEqual(state.params.sortings, sortings)) return;
      state.params.sortings = sortings
      state.loadingTime = undefined;
    },
    setParams(state, params) {
      if (deepEqual(state.params, params)) return;
      state.params = params;
      state.loadingTime = undefined;      
    },
    setRows(state, rows) {
      state.loadingTime = new Date();
      state.rows = rows;
    },
    setTotalCount(state, totalCount) {
      state.loadingTime = new Date();
      state.totalCount = totalCount;
    },
    setLoadingTime: (state, time) => state.loadingTime = time,
  },
  actions: {
    find: ({ rootState }, key) => new Promise<IUserData>((resolve, reject) => {      
      rootState.clients.authorized.get(`${apiPath}/${key}`).then(
        (success: IJsonResponse<IUserData>) => resolve(success.payload!),
        (failure: IJsonResponse<any>) => reject(failure.message)
      );
    }),
    query: ({ rootState }, params) => new Promise((resolve, reject) => {
      rootState.clients.authorized.get(apiPath, { params }).then(
        (success: IJsonResponse<any>) => resolve(success.payload),
        (failure: IJsonResponse<any>) => reject(failure.message)
      )
    }),
  }
}

export default module;