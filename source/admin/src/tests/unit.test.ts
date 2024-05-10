import { describe, it, expect, beforeAll } from "vitest";
import { mount } from "@vue/test-utils";
import { helper } from "@/utils/helper";

import { Application, Policy, createI18n } from "@cloudfun/core";
import model from "@/models";
import messages from "@/locales";
import router from "@/router";
import App from "@/App.vue";

beforeAll(() => {
    const app = new Application(
        App, 
        model, 
        new Policy(model, { router}),
        createI18n({ locale: 'zh-Hant', fallbackLocale: 'zh-Hant', messages })
    );
});

describe('Function Tests', () => {
    it('Truncate text with ellipsis', () => expect(helper.cutText("Test Text", 5)).toBe("Test..."));
});

describe('Component Tests', () => {
    let wrapper = mount(App);
    it('App is a router view', () => expect(wrapper.html()).toContain("<router-view></router-view>"));
    // 斷言其中存在指定元件
    // expect(wrapper.findComponent(App)).toBe(true);
    // 斷言取得資料長度為3
    // const items = wrapper.findAll('li');
    // expect(items.length).toBe(3);
    // 斷言取得資料內容為tets
    // expect(items[0].text()).toBe('test');
});