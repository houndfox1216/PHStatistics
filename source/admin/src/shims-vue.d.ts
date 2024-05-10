declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<unknown, unknown, any>
  export default component
}

declare module '*.png';

declare module "@sipec/vue3-tags-input";

interface ImportMeta {
  readonly env: ImportMetaEnv
}