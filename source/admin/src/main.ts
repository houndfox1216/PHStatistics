import context, { Application, MessageType, Policy, createI18n } from "@cloudfun/core";

import model from "./models";
import router from "./router";
import sitemap from "./sitemap";
import messages from "./locales";

import Toastify from "toastify-js";

import '@fullcalendar/core/vdom'; // solves problem with Vite
import "./assets/css/app.css";

import globalComponents from "./global-components";
import utils from "./utils";
import App from "./App.vue";

import './plugins/devextreme';

const app = new Application( App, model, new Policy( 
    model, { 
        router, 
        sitemap, 
        loginRoute: '/login', 
        changePasswordRoute: '/profile/change-password', 
        guard: (_to, _from, next) => { 
            model.dispatch('configuration/read'); 
            next(); 
        } 
    }),
    createI18n({ locale: 'zh-Hant', fallbackLocale: 'zh-Hant', messages })
);

// 將 JS 攔截到的錯誤轉給 CloudfunVue Messenger
window.onerror = (message, source, lineno, colno) => {
    if (message === 'ResizeObserver loop limit exceeded') return false;
    app.send('log', {
        createdTime: new Date(),
        type: MessageType.Error,
        content: app.user ? `[${app.user.Name}] \r\n${message}\r\n    at ${source}:${lineno}:${colno}` : `\r\n${message}\r\n    at ${source}:${lineno}:${colno}`
    });
    return true;
}

// 將 Vue.js 攔截到的錯誤轉給 CloudfunVue Messenger
app.config.errorHandler = (error: any, instance, info) => {
    let content = app.user ? `[${app.user.Name}] ${info}: ` : `${info}: `;
    if (typeof error === 'string') content += error;
    else content += `${error.message}\r\n${error.stack}`;
    console.log(error);
    app.send('log', {
        createdTime: new Date(),
        type: MessageType.Error,
        content,
    });
}

// 設定信差中介程序
context.setMiddlewares({
    info: (message) => Toastify({
        text: `${typeof message === 'string' ? 'Information' : message.subject || 'Information'}: ${typeof message === 'string' ? message : message.content}`,
        close: true,
        gravity: 'bottom',
        position: 'left',
        style: { 
            background: 'rgb(var(--color-success))',
            color: "white",
            fontSize: '1rem',
        },
        className: "toastify-content",
        stopOnFocus: true,
    }).showToast(),
    warning: (message) => Toastify({
        text: `${typeof message === 'string' ? 'Warning' : message.subject || 'Warning'}: ${typeof message === 'string' ? message : message.content}`,
        close: true,
        gravity: 'bottom',
        position: 'left',
        style: { 
            background: 'rgb(var(--color-pending))',
            color: "white",
            fontSize: '1rem',
        },
        className: "toastify-content",
        stopOnFocus: true,
    }).showToast(),
    error: (message) => Toastify({
        text: `${typeof message === 'string' ? 'Error' : message.subject || 'Error'}: ${typeof message === 'string' ? message : message.content}`,
        duration: -1,
        close: true,
        gravity: 'bottom',
        position: 'left',
        style: { 
            background: 'rgb(var(--color-danger))',
            color: "white",
            fontSize: '1rem',
        },
        className: "toastify-content",
        stopOnFocus: true,
    }).showToast(),
    'application.error': (error) => console.log(error),
})
  
globalComponents(app);
utils(app);

app.run("#app");