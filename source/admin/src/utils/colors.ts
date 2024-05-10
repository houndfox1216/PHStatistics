import { App } from "vue";
import {
  inherit,
  current,
  transparent,
  black,
  white,
  slate,
  gray,
  zinc,
  neutral,
  stone,
  red,
  orange,
  amber,
  yellow,
  lime,
  green,
  emerald,
  teal,
  cyan,
  sky,
  blue,
  indigo,
  violet,
  purple,
  fuchsia,
  pink,
  rose,
} from "tailwindcss/colors";
import { helper } from "@/utils/helper";

const el = getComputedStyle(document.body);
const mainColors = {
  ...helper.toRGB({
    inherit,
    current,
    transparent,
    black,
    white,
    slate,
    gray,
    zinc,
    neutral,
    stone,
    red,
    orange,
    amber,
    yellow,
    lime,
    green,
    emerald,
    teal,
    cyan,
    sky,
    blue,
    indigo,
    violet,
    purple,
    fuchsia,
    pink,
    rose,
  }),
  primary: (opacity = 1) =>
    `rgb(${el.getPropertyValue("--color-primary")} / ${opacity})`,
  secondary: (opacity = 1) =>
    `rgb(${el.getPropertyValue("--color-secondary")} / ${opacity})`,
  success: (opacity = 1) =>
    `rgb(${el.getPropertyValue("--color-success")} / ${opacity})`,
  info: (opacity = 1) =>
    `rgb(${el.getPropertyValue("--color-info")} / ${opacity})`,
  warning: (opacity = 1) =>
    `rgb(${el.getPropertyValue("--color-warning")} / ${opacity})`,
  pending: (opacity = 1) =>
    `rgb(${el.getPropertyValue("--color-pending")} / ${opacity})`,
  danger: (opacity = 1) =>
    `rgb(${el.getPropertyValue("--color-danger")} / ${opacity})`,
  light: (opacity = 1) =>
    `rgb(${el.getPropertyValue("--color-light")} / ${opacity})`,
  dark: (opacity = 1) =>
    `rgb(${el.getPropertyValue("--color-dark")} / ${opacity})`,
  slate: {
    50: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-slate-50")} / ${opacity})`,
    100: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-slate-100")} / ${opacity})`,
    200: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-slate-200")} / ${opacity})`,
    300: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-slate-300")} / ${opacity})`,
    400: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-slate-400")} / ${opacity})`,
    500: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-slate-500")} / ${opacity})`,
    600: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-slate-600")} / ${opacity})`,
    700: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-slate-700")} / ${opacity})`,
    800: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-slate-800")} / ${opacity})`,
    900: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-slate-900")} / ${opacity})`,
  },
  darkmode: {
    50: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-darkmode-50")} / ${opacity})`,
    100: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-darkmode-100")} / ${opacity})`,
    200: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-darkmode-200")} / ${opacity})`,
    300: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-darkmode-300")} / ${opacity})`,
    400: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-darkmode-400")} / ${opacity})`,
    500: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-darkmode-500")} / ${opacity})`,
    600: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-darkmode-600")} / ${opacity})`,
    700: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-darkmode-700")} / ${opacity})`,
    800: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-darkmode-800")} / ${opacity})`,
    900: (opacity = 1) =>
      `rgb(${el.getPropertyValue("--color-darkmode-900")} / ${opacity})`,
  },
};

const install = (app: App): void => {
  app.config.globalProperties.$c = () => {
    return mainColors;
  };
};

export { install as default, mainColors as colors };
