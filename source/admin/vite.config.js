import { defineConfig, loadEnv } from "vite";
import vue from "@vitejs/plugin-vue";
import checker from 'vite-plugin-checker';
import path from "path";
// import basicSsl from '@vitejs/plugin-basic-ssl'

// https://vitejs.dev/config/
export default ({ mode }) => {
  process.env = {...process.env, ...loadEnv(mode, process.cwd())};
  return defineConfig({
    define: {
      'process.env': process.env
    },
    plugins: [
      vue(),
      checker({ typescript: true }),
      // basicSsl(),
    ],
    optimizeDeps: {
      include: ['@cloudfun/ckeditor5-classic-build'],
    },
    build: {
      commonjsOptions: { exclude: ['@cloudfun/ckeditor5-classic-build'] },
    },
    resolve: {
      dedupe: [
        'vue'
      ],
      alias: {
        "vue": "vue/dist/vue.esm-bundler.js",
        "vue-i18n": "vue-i18n/dist/vue-i18n.esm-browser.prod.js",
        "devextreme/ui": 'devextreme/esm/ui',
        'inferno':  'inferno/dist/index.esm.js',
        "@": path.resolve(__dirname, "./src"),
      },
    },
    server: {
      https: false,
      proxy: {
        '/resources': { target: process.env.VITE_SERVICE_URI, secure: false },
        '/files': { target: process.env.VITE_SERVICE_URI, secure: false },
      }
    }
  });
};
