import context, { ModelState, IJsonResponse, Condition, Operator } from "@cloudfun/core";
import { Module } from "vuex";
import CustomStore from 'devextreme/data/custom_store';

const apiPath = "/User";

const module: Module<CustomStore, ModelState> = {
  namespaced: true,
  getters: {
    store: state => state,
  },
  actions: {
    getStore: ({ rootState, state }, getCustomParameters?: () => any) => {
      return "load" in state ? state : state = new CustomStore({
        key: 'Id',
        load(options) {
          const searchCondition = new Condition();
          if ("searchValue" in options && options.searchValue) {
            const operation = options.searchOperation === "startswith" ? Operator.StartsWith : Operator.Contains;
            const value = options.searchValue;
            if ("searchExpr" in options && options.searchExpr) {
              const columns = Array.isArray(options.searchExpr) ? options.searchExpr : [options.searchExpr];
              columns.forEach(e => {
                const column = typeof e === 'string' ? e : e();
                searchCondition.or(column, operation, value);
              });
            }
          }
          let params = "?";
          if (getCustomParameters) {
            const customParams = getCustomParameters();
            if (!searchCondition.isNone) {
              if (customParams.condition) customParams.condition = searchCondition.and(customParams.condition);
              else customParams.condition = searchCondition;
            }
            if (customParams) for (const key in customParams) params += `${key}=${JSON.stringify(customParams[key])}&`;
          } else {
            params += `condition=${JSON.stringify(searchCondition)}&`;
          }
          ['skip','take','requireTotalCount','requireGroupCount','sort','filter','totalSummary','group','groupSummary'].forEach(key => {
            if (key in options) {
              const value = options[key as keyof typeof options];
              if (value === 0 || value === false || value) params += `${key}=${JSON.stringify(value)}&`;
            }
          });
          params = params.slice(0, -1);
          return rootState.clients.authorized.get(`${apiPath}/Load${params}`).then(
            (success: IJsonResponse<any>) => success.payload,
            (failure: IJsonResponse<any>) => context.send('error', { 
              subject: context.root!.i18n.global.t("model.error.load"), 
              content: failure.message 
            })
          );
        }, 
        byKey(key) {
          return rootState.clients.authorized.get(`${apiPath}/${key}`).then(
            (success: IJsonResponse<any>) => success.payload,
            (failure: IJsonResponse<any>) => context.send('error', { 
              subject: context.root!.i18n.global.t("model.error.read"), 
              content: failure.message 
            })
          );
        },
        insert(values) {
          return rootState.clients.authorized.post(apiPath, values).then(
            (success: IJsonResponse<any>) => success.payload,
            (failure: IJsonResponse<any>) => context.send('error', { 
              subject: context.root!.i18n.global.t("model.error.insert"), 
              content: failure.message 
            })
          );
        }, 
        update(key, values) {
          values["Id"] = key;
          return rootState.clients.authorized.put(apiPath, values).then(
            (success: IJsonResponse<any>) => success.payload,
            (failure: IJsonResponse<any>) => context.send('error', { 
              subject: context.root!.i18n.global.t("model.error.update"), 
              content: failure.message 
            })
          );
        }, 
        remove(id) {
          return rootState.clients.authorized.delete(apiPath, { params: { id } }).then(
            (success: IJsonResponse<any>) => success.payload,
            (failure: IJsonResponse<any>) => context.send('error', { 
              subject: context.root!.i18n.global.t("model.error.remove"), 
              content: failure.message 
            })
          );
        }
      });
    },
    find: ({ rootState, state, getters }, key) => new Promise((resolve, reject) => {
      rootState.clients.authorized.get(`${apiPath}/${key}`).then(
        (success: IJsonResponse<any>) => resolve(success.payload!),
        (failure: IJsonResponse<any>) => reject(failure.message)
      );
    }),
    insert: ({ rootState, commit }, data) => new Promise((resolve, reject) => {
      rootState.clients.authorized.post(apiPath, data).then(
        (success: IJsonResponse<any>) => {
          resolve(success.payload!);
        },
        (failure: IJsonResponse<any>) => reject(failure.message)
      )
    }),
    update: ({ rootState, commit }, data) => new Promise((resolve, reject) => {
      rootState.clients.authorized.put(apiPath, data).then(
        (success: IJsonResponse<any>) => {
          resolve(success.payload!);
        },
        (failure: IJsonResponse<any>) => reject(failure.message)
      );
    }),
    delete: ({ rootState, commit }, id) => new Promise((resolve, reject) => {
      rootState.clients.authorized.delete(apiPath, { params: { id } }).then(
        (success: IJsonResponse<any>) => {
          resolve(success.payload);
        },
        (failure: IJsonResponse<any>) => reject(failure.message)
      );
    }),
    save: ({ rootState, commit }, changes) => new Promise((resolve, reject) => {
      rootState.clients.authorized.post(`${apiPath}/Save`, changes).then(
        (success: IJsonResponse<any>) => {
          resolve(success.payload);
        },
        (failure: IJsonResponse<any>) => reject(failure.message)
      );
    }),
    query: ({ rootState }, params) => new Promise((resolve, reject) => {
      rootState.clients.authorized.get(apiPath, { params }).then(
        (success: IJsonResponse<any>) => resolve(success.payload),
        (failure: IJsonResponse<any>) => reject(failure.message)
      )
    }),
    changePassword: ({ rootState }, params) => new Promise((resolve, reject) => {      
      rootState.clients.authorized.post(`${apiPath}/ChangePassword`, params).then(
        (success: IJsonResponse<any>) => {          
          resolve(success.payload);
        },
        (failure: IJsonResponse<any>) => reject(failure.message)
      );
    }),
  }
}

export default module;