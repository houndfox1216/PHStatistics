import { defineConfig } from 'vitest/config';
import { fileURLToPath, URL } from 'node:url';
import vue from "@vitejs/plugin-vue"

export default defineConfig({
  resolve: { // https://vitejs.dev/config/#resolve-alias
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  plugins: [ vue() ],
  test: { // https://vitest.dev/guide/#configuring-vitest
    globals: true,
    environment: 'jsdom',
  },
});