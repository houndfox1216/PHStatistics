/* Fromat: {  culture-code: localization-json } */

import { locale, loadMessages } from 'devextreme/localization';

import zh_Hant from "./zh-Hant.json";
import en_US from "./en-US.json";

const localization = {
  'zh-Hant': zh_Hant,
  'en-US': en_US,
}

loadMessages({ "zh-Hant": zh_Hant.DevExtreme });
loadMessages({ "en-US": en_US.DevExtreme });

let currentLocale = localStorage.getItem("locale")
if (!currentLocale) {
  currentLocale = "zh-Hant";
  localStorage.setItem("locale", currentLocale);
}
locale(currentLocale);

export default localization
