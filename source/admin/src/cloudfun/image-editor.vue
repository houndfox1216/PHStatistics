<template>
  <div class="preview bg-checkerboard" @click="onPreviewClick">
    <img v-if="uri || defaultUri" :src="uri || defaultUri" class="preview-image" />
    <Placeholder v-else :width="limitedWidth" :height="limitedHeight" />
    <DxPopup
      v-model:visible="fileManagerVisible"
      :width="850"
      :height="668"
      :resize-enabled="true"
      :drag-enabled="true"
      :show-close-button="true"
      :show-title="true"
      :enable-body-scroll="false"
      container="body"
      :title="$t('component.imageEditorPopupTitle')"
      :full-screen="fullscreenable"
    >
      <DxToolbarItem toolbar="top" location="after">
        <MinimizeIcon v-if="fullscreenable" class="-mr-4 cursor-pointer" @click="fullscreenable=false" />
        <MaximizeIcon v-else class="-mr-4 cursor-pointer" @click="fullscreenable=true" />
      </DxToolbarItem>
      <DxToolbarItem
        widget="dxButton"
        toolbar="bottom"
        location="after"
        :visible="submitButtonVisible"
        :options="submitButtonOptions"
      />
      <DxFileManager
        :current-path="currentPath"
        :file-system-provider="fileProvider"
        :selection-mode="selectionMode"
        :allowed-file-extensions="allowedFileExtensions"
        :on-selected-file-opened="openFile"
        @selection-changed="onSelectionChanged"
      >
        <DxPermissions
          :create="create"
          :copy="copy"
          :move="move"
          :delete="remove"
          :rename="rename"
          :upload="upload"
          :download="download"
        />
      </DxFileManager>
    </DxPopup>
    <DxPopup
      v-model:visible="previewVisible"
      :title="openedFile.name"
      :width="350"
      :height="350"
      :hide-on-outside-click="true"
      :resize-enabled="true"
      :drag-enabled="true"
      :enable-body-scroll="false"
      :full-screen="previewFullscreenable"
    >
      <DxToolbarItem toolbar="top" location="after">
        <MinimizeIcon v-if="previewFullscreenable" class="-mr-4 cursor-pointer" @click="previewFullscreenable=false" />
        <MaximizeIcon v-else class="-mr-4 cursor-pointer" @click="previewFullscreenable=true" />
      </DxToolbarItem>
      <img class="w-full h-full object-scale-down bg-checkerboard" :src="openedFile.dataItem.url">
    </DxPopup>
  </div>
</template>
  
<style>
.preview-image {
    object-fit: scale-down;
    width: 100%;
    height: 100%;
    border: 1px solid rgb(var(--color-slate-300));
    border-radius: 0.5rem;
}

.preview {
  display: flex;
  position: relative;
  align-items: center;
  justify-content: center;
  width: v-bind(computedWidth);
  height: v-bind(computedHeight);
  border-radius: 0.5rem;
}
</style>

<script lang="ts">
  import context, { ref, PropType, defineComponent } from "@cloudfun/core";

  import "@cloudfun/placeholder/dist/placeholder.css"
  import Placeholder from '@cloudfun/placeholder';

  import { DxPopup, DxToolbarItem } from 'devextreme-vue/popup';
  import { DxFileManager, DxPermissions } from 'devextreme-vue/file-manager';
  import RemoteFileSystemProvider from 'devextreme/file_management/remote_provider';

  export default defineComponent({
    components: { 
      Placeholder, 
      DxPopup, 
      DxToolbarItem,
      DxFileManager, 
      DxPermissions 
    },
    props: {
      disabled: { type: Boolean, default: false },
      width: Number,
      height: Number,
      limitedWidth: Number,
      limitedHeight: Number,
      defaultUri: String,
      fileSystemProvider: { 
        type: [Function, Object] as PropType<RemoteFileSystemProvider | ((limitedWidth?: number, limitedHeight?: number) => string)>, 
        required: true 
      },
      create: Boolean,
      copy: Boolean,
      move: Boolean,
      remove: Boolean,
      rename: Boolean,
      upload: Boolean,
      download: Boolean,
      currentPath: String,
      allowedFileExtensions: Array as PropType<string[]>,
      selectionMode: { type: String as PropType<"single" | "multiple">, default: 'single' },
      value: { type: [String, Array] as PropType<string | string[]> },
      modelValue: { type: [String, Array] as PropType<string | string[]> },
    },
    setup(props, { emit }) {
      const fileManager = ref<any>();
      const fileManagerVisible = ref(false);
      return {
        fileManager,
        fileManagerVisible,
        fileProvider: typeof props.fileSystemProvider === 'function'
          ? new RemoteFileSystemProvider({ endpointUrl: props.fileSystemProvider(props.limitedWidth, props.limitedHeight) })
          : props.fileSystemProvider,
        openedFile: ref<any>({ dataItem: {} }),
        previewVisible: ref(false),
        fullscreenable: ref(false),
        previewFullscreenable: ref(false),
        submitButtonVisible: ref(false),
        submitButtonOptions: {
          text: context.root!.i18n.global.t('component.imageEditorPopupSubmit'),
          onClick: () => {
            if (fileManager.value) {
              let values = fileManager.value.getSelectedItems().map((e: any) => e.dataItem.url);
              if (values.length) {
                switch(props.selectionMode) {
                  case 'single': 
                    emit("change", values[0]);
                    emit("update:modelValue", values[0]);
                    break;
                  default:
                    emit("change", values);
                    emit("update:modelValue", values);
                    break;
                }
                fileManagerVisible.value = false;
              }
            }
          },
        },
      };
    },
    computed: {
      uri() {
        if (this.modelValue !== undefined) return Array.isArray(this.modelValue) ? this.modelValue[0] : this.modelValue;
        return Array.isArray(this.value) ? this.value[0] : this.value;
      },
      computedWidth() {
        return this.width !== undefined ? `${this.width}px` : "100%"
      },
      computedHeight() {
        return this.height !== undefined ? `${this.height}px` : "100%"
      },
    },
    methods: {
      onPreviewClick() {
        if (this.disabled) return;
        this.fileManagerVisible = true;
      },
      onSelectionChanged(e: any) {
        this.fileManager ??= e.component;
        this.submitButtonVisible = e.selectedItems.length > 0;
      },
      openFile(e: any) {
        this.openedFile = e.file;
        this.previewVisible = true;
      }
    }
  });
  </script>
  