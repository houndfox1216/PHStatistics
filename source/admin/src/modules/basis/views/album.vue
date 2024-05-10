<template>
  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-12 lg:col-span-2 2xl:col-span-1 sm:hidden flex mt-6">
      <h2 class="intro-y text-lg font-medium mr-auto pl-2">{{($breadcrumb[$breadcrumb.length-1] as ISitemapNode).title}}</h2>
    </div>
    <!-- BEGIN: Content -->
    <div class="intro-y col-span-12 box mt-0 sm:mt-6 p-3">
      <DxDataGrid
        ref="grid"
        :show-borders="true"
        :data-source="albumStore"
        :remote-operations="true"
        :allow-column-resizing="true"
        :allow-column-reordering="true"
        :row-alternation-enabled="true"
        @init-new-row="onGridAdding"
        @editing-start="onGridEditing"
        @exporting="onGridExporting"
        @toolbarPreparing="(e: any) => e.toolbarOptions.items.unshift({ location: 'after', widget: 'dxButton', options: { 
          icon: 'refresh', 
          hint: '重置狀態', 
          onClick: function() { 
            e.component.state(null);
            e.component.refresh();
          }
        }})"
      >
        <DxStateStoring :enabled="true" type="localStorage" storage-key="album-grid" />
        <DxSelection mode="multiple" />
        <DxExport :enabled="true" :allow-export-selected-data="true" />
        <DxGroupPanel :visible="true"/>
        <DxColumnChooser :enabled="true" />
        <DxSearchPanel :visible="true" />
        <DxHeaderFilter :visible="true"/>
        <DxFilterRow :visible="true"/>
        <DxPager :show-page-size-selector="true" :allowed-page-sizes="[10, 20, 50]" :show-navigation-buttons="true" :show-info="true" />
        <DxRowDragging
          :allow-reordering="true"
          :on-reorder="onGridReorder"
        />
        <DxColumn data-field="Number" caption="代碼" :editor-options="{ maxLength: 32, placeholder: '系統自動產生', readOnly: true }" />
        <DxColumn data-field="Title" caption="標題" :editor-options="{ maxLength: 128 }">
          <DxRequiredRule />
        </DxColumn>
        <DxColumn data-field="StartDate" data-type="date" caption="開始日期" :width="120" />
        <DxColumn data-field="EndDate" data-type="date" caption="結束日期" :width="120" />
        <DxColumn data-field="Published" caption="已發佈">
          <DxLookup :data-source="[{ label: '是', value: true }, { label: '否', value: false }]" display-expr="label" value-expr="value" />
        </DxColumn>
        <DxColumn data-field="Ordinal" caption="排序值" data-type="number" :width="100" sort-order="asc" />
        <DxEditing :allow-adding="true" :allow-updating="true" :allow-deleting="true" :use-icons="true" mode="popup">
          <DxGridPopup 
            :title="$t('app.basis.album.modal.title')"
            :show-title="true" 
            :width="700" 
            :height="604" 
            :minWidth="578" 
            :resizeEnabled="true" 
            :full-screen="fullscreenable"
            @contentReady="onGridModalContentReady"
            @resize="onGridModalResize" 
            >
            <DxToolbarItem toolbar="top" location="after">
              <MinimizeIcon v-if="fullscreenable" class="-mr-4 cursor-pointer" @click="fullscreenable=false" />
              <MaximizeIcon v-else class="-mr-4 cursor-pointer" @click="fullscreenable=true" />
            </DxToolbarItem>
            <DxToolbarItem 
              widget="dxButton" 
              toolbar="bottom" 
              location="after" 
              :options="{ 
                text: $t('DevExtreme.dxDataGrid-editingSaveRowChanges'),
                onClick: () => grid.instance.saveEditData(),
              }"
            />
            <DxToolbarItem 
              widget="dxButton" 
              toolbar="bottom" 
              location="after" 
              :options="{ 
                text: $t('DevExtreme.dxDataGrid-editingCancelRowChanges'),
                onClick: () => { 
                  if (grid.instance.hasEditData()) grid.instance.refresh(true);
                  grid.instance.cancelEditData(); 
                },
              }"
            />
          </DxGridPopup>
          <DxGridForm label-mode="static" :col-count="4">
            <DxItem data-field="Number" :col-span="2" />
            <DxItem data-field="Published" :label="{ visible: false }" />
            <DxItem data-field="Ordinal" />
            <DxItem data-field="Title" :col-span="4" />
            <DxItem data-field="StartDate" :col-span="2" />
            <DxItem data-field="EndDate" :col-span="2" />
            <DxItem :col-span="4" item-type="group">
              <fieldset class="dx-static-label-border">
                <legend class="dx-static-label">相片集</legend>
                <GalleryEditor
                  ref="editor"
                  :navigation="true"
                  :width="editorWidth"
                  v-model="editingRow.Pictures"
                  @add="onSliderEdit"
                  @edit="onSliderEdit"
                  @save="onSliderSave"
                  @remove="onSliderRemove"
                >
                  <template #default="{ data, save, visible, setVisible }">
                    <DxPopup
                      :title="`${data?.Id ? '編輯' : '新增'}相片`"
                      :drag-enabled="true"
                      :resize-enabled="true"
                      :show-close-button="true"
                      :show-title="true"
                      :width="600"
                      :height="430"
                      container="body"
                      :visible="visible"
                      :full-screen="formFullscreenable"
                      @update:visible="(value: boolean) => setVisible(value)"
                    >
                      <DxToolbarItem toolbar="top" location="after">
                        <MinimizeIcon v-if="formFullscreenable" class="-mr-4 cursor-pointer" @click="formFullscreenable=false" />
                        <MaximizeIcon v-else class="-mr-4 cursor-pointer" @click="formFullscreenable=true" />
                      </DxToolbarItem>
                      <DxToolbarItem
                        widget="dxButton"
                        toolbar="bottom"
                        location="after"
                        :options="{
                          text: '確認',
                          onClick: () => {
                            const form = pictureForm.instance;
                            const result = form.validate();
                            if (result.isValid) save(data); 
                          }
                        }"
                      />
                      <div class="w-full text-center mb-3">
                        <ImageEditor 
                          :current-path="editingRow?.Id ? `albums/234234` : 'photos'"
                          :selection-mode="data?.Id ? 'single' : 'multiple'"
                          :allowed-file-extensions="['.png', '.jpg', '.svg']"
                          :default-uri="'/images/camera.svg'"
                          :height="150" 
                          :file-system-provider="fileSystemProvider"
                          :create="true"
                          :copy="true"
                          :move="true"
                          :remove="true"
                          :rename="true"
                          :upload="true"
                          :download="true"
                          :value="data?.Uri"
                          @change="(value: any) => { selectedFiles = Array.isArray(value) ? value : [value]; data.Uri = selectedFiles[0]; }"
                        />
                        <ul v-if="selectedFiles.length > 1">
                          <li v-for="file in selectedFiles" class="border rounded-sm px-1 flex" :class="{ 'bg-primary/30': file == data?.Uri }" @click.stop="data.Uri = file">
                            <span class="truncate">{{ file }}</span>
                            <button 
                              class="ml-auto" 
                              @click.stop="selectedFiles = selectedFiles.filter(e => e != file); if (!selectedFiles.some(e => e == file)) data.Uri = selectedFiles[0];"
                            >
                              <FontAwesome icon="close" class="text-danger w-3 h3" />
                            </button>
                          </li>
                        </ul>
                      </div>
                      <DxForm 
                        ref="pictureForm"
                        :form-data="data" 
                        :col-count="4"
                      >
                        <DxFormItem data-field="Name" :col-span="3" :label="{ text: '名稱' }" />
                        <DxFormItem data-field="Ordinal" :label="{ text: '排序值' }" />
                        <DxFormItem data-field="Remark" :col-span="4" editor-type="dxTextArea" :label="{ text: '備註' }" />
                      </DxForm>
                    </DxPopup>
                  </template>
                </GalleryEditor>
              </fieldset>
            </DxItem>
          </DxGridForm>
        </DxEditing>
      </DxDataGrid>
    </div>
    <!-- END: Content -->
  </div>
</template>

<script lang="ts">
import { defineComponent, ISitemapNode, ref } from '@cloudfun/core'

import { Workbook } from 'exceljs';
import { saveAs } from 'file-saver';
import { DxTextArea  } from 'devextreme-vue';
import CustomStore from 'devextreme/data/custom_store';
import { exportDataGrid } from 'devextreme/excel_exporter';
import { EditingStartEvent } from 'devextreme/ui/data_grid';
import { DxForm, DxItem as DxFormItem } from 'devextreme-vue/form';
import { DxPopup, DxToolbarItem } from 'devextreme-vue/popup';
import { DxSwitch } from 'devextreme-vue/switch';
import {
  DxDataGrid,
  DxStateStoring,
  DxSelection,
  DxEditing,
  DxPopup as DxGridPopup, 
  DxForm as DxGridForm,
  DxGroupItem,
  DxItem,
  DxExport,
  DxGroupPanel,
  DxColumnChooser,
  DxSearchPanel,
  DxHeaderFilter,
  DxFilterRow,
  DxPager,
  DxRowDragging,
  DxColumn,
  DxLookup,
  DxRequiredRule,
  DxEmailRule,
  DxPatternRule,
  DxCustomRule,
} from 'devextreme-vue/data-grid';

import GalleryEditor from "@/cloudfun/gallery-editor.vue";
import ImageEditor from '@/cloudfun/image-editor.vue';

export default defineComponent({
  components: {
    DxDataGrid,
    DxStateStoring,
    DxSelection,
    DxEditing,
    DxGridPopup,
    DxGridForm,
    DxGroupItem,
    DxItem,
    DxExport,
    DxGroupPanel,
    DxColumnChooser,
    DxSearchPanel,
    DxHeaderFilter,
    DxFilterRow,
    DxPager,
    DxRowDragging,
    DxColumn,
    DxLookup,
    DxRequiredRule,
    DxEmailRule,
    DxPatternRule,
    DxCustomRule,
    DxTextArea,
    DxPopup,
    DxToolbarItem,
    GalleryEditor,
    ImageEditor,
    DxForm,
    DxFormItem,
    DxSwitch,
  },
  setup () {
    return {
      grid: ref<any>({}),
      albumStore: ref<CustomStore>(),
      editingRow: ref<any>({ Photo: {}, Address: {} }),
      editor: ref<any>({}),
      editorWidth: ref<string>(),
      pictureForm: ref<any>({}),
      fullscreenable: ref(false),
      formFullscreenable: ref(false),
      selectedFiles: ref<string[]>([]),
    };
  },
  async beforeMount() {
    this.albumStore = await this.$model.dispatch('album/getStore');
  },
  computed: {
    fileSystemProvider() {
      return (limitedWidth?: number, limitedHeight?:number) => `${import.meta.env.VITE_SERVICE_URI}/api/File/Execute?limited=${limitedWidth}x${limitedHeight}`
    }
  },
  methods: {
    onGridAdding(e: any) {
      this.editingRow = e.data;
    },
    onGridEditing(e: EditingStartEvent) {
      this.editingRow = e.data;
    },
    onGridExporting(e: any) {
      const workbook = new Workbook();
      const worksheet = workbook.addWorksheet('Album');
      exportDataGrid({ component: e.component, worksheet, autoFilterEnabled: true }).then(() => {
        workbook.xlsx.writeBuffer().then((buffer) => {
          saveAs(new Blob([buffer], { type: 'application/octet-stream' }), 'album.xlsx');
        });
      });
      e.cancel = true;
    },
    onGridReorder(e: any) {
      if (e.toIndex === -1) e.cancel = true;
      else {
        const source = e.itemData;
        const visibleRows = e.component.getVisibleRows();
        const targetIndex = e.dropInsideItem || e.fromIndex > e.toIndex ? e.toIndex : e.toIndex + 1;
        const target = visibleRows[targetIndex < visibleRows.length ? targetIndex : visibleRows.length-1].data;
        const isAfter = !e.dropInsideItem && targetIndex >= visibleRows.length;
        this.$model.dispatch('album/reorder', { source: source.Id, target: target.Id, isAfter }).then(
          () => e.component.refresh(),
          failure => {
            this.$send('error', { 
              subject: this.$tc("model.error.update"), 
              content: failure.message 
            });
            e.cancel = true;
          }
        );
      }
    },
    async onGridModalContentReady(e: any) {
      const entity = this.editingRow.Id ? await this.$model.dispatch('album/find', this.editingRow.Id) : undefined;
      if (entity) Object.assign(this.editingRow, entity);
      if (this.editingRow.Pictures) this.editingRow.Pictures = this.editingRow.Pictures.sort((a: any, b: any) => a.Ordinal - b.Ordinal);
      if (e.component._$content.length > 0) this.editorWidth = `${e.component._$content[0].clientWidth-100}px`;
      this.editor.reload(this.editingRow.Pictures);
    },
    onGridModalResize(size: { width: number }) {
      this.editorWidth = `${size.width - 100}px`;
    },
    onSliderEdit(image: any, callback: any) {
      this.selectedFiles = [];
      image.AlbumId = this.editingRow.Id;
      callback();
    },
    async onSliderRemove(image: any, callback: any) {
      await this.$model.dispatch('picture/delete', image.Id);
      callback();
    },
    async onSliderSave(image: any, callback: any) {
      const promises = [];
      if (image.Id) promises.push(this.$model.dispatch('picture/update', image));
      else this.selectedFiles.forEach(file => promises.push(this.$model.dispatch('picture/insert', {...image, Uri: file})));
      await Promise.all(promises).then(
        () => {
          this.$model.dispatch('album/find', this.editingRow.Id).then(
            (entity) => {
              this.editor.reload(entity.Pictures.sort((a: any, b: any) => a.Ordinal - b.Ordinal))
            },
            reason => { this.$send('error', { subject: '重載失敗', content: reason }) }
          )
        },
        reason => { this.$send('error', { subject: '保存失敗', content: reason }) }
      )
      callback()
    },
  }
})
</script>
