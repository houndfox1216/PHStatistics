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
        :data-source="liveSourceStore"
        :remote-operations="true"
        :allow-column-resizing="true"
        :allow-column-reordering="true"
        :row-alternation-enabled="true"
        @init-new-row="onGridAdding"
        @editing-start="onGridEditing"
        @edit-canceled="pausePlayer"
        @saving="pausePlayer"
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
        <DxStateStoring :enabled="true" type="localStorage" storage-key="live-source-grid" />
        <DxExport :enabled="true" />
        <DxGroupPanel :visible="true"/>
        <DxColumnChooser :enabled="true" />
        <DxSearchPanel :visible="true" />
        <DxHeaderFilter :visible="true"/>
        <DxFilterRow :visible="true"/>
        <DxRowDragging
          :allow-reordering="true"
          :on-reorder="onGridReorder"
        />
        <DxPager :show-page-size-selector="true" :allowed-page-sizes="[10, 20, 50]" :show-navigation-buttons="true" :show-info="true" />
        <DxColumn data-field="Title.DefaultText" caption="標題" />
        <DxColumn data-field="Entry" caption="串流名稱" :allow-editing="false" :editor-options="{ placeholder: '系統自動產生' }">
          <DxPatternRule :pattern="new RegExp('^[a-zA-Z0-9_-]{8,}$')" message="須8碼以上僅含英數字、減號宇底線" />
        </DxColumn>
        <DxColumn data-field="Token" caption="令牌" :editor-options="{ placeholder: '未輸入時將由系統產生' }" :visible="false">
          <DxPatternRule :pattern="new RegExp('^[a-zA-Z0-9_-]{8,}$')" message="須8碼以上僅含英數字、減號宇底線" />
        </DxColumn>
        <DxColumn data-field="StartTime" caption="開始時間" data-type="datetime" :editor-options="{ displayFormat: 'yyyy/MM/dd HH:mm:ss' }" :width="140"/>
        <DxColumn data-field="EndTime" caption="結束時間" data-type="datetime" :editor-options="{ displayFormat: 'yyyy/MM/dd HH:mm:ss' }" :width="140"/>
        <DxColumn data-field="Published" caption="已發佈" :width="100">
          <DxLookup :data-source="[{ label: '是', value: true }, { label: '否', value: false }]" display-expr="label" value-expr="value" />
        </DxColumn>
        <DxColumn data-field="PublisherId" caption="發佈者" :visible="false">
          <DxLookup :data-source="userStore" :search-enabled="true" search-expr="Name" display-expr="Name" value-expr="Id" />
        </DxColumn>
        <DxColumn data-field="Ordinal" caption="排序值" data-type="number" :width="100" sort-order="asc" />
        <DxColumn data-field="PushUri" caption="推播參數" edit-cell-template="push-uri-template" :visible="false" />
        <template #push-uri-template="{ data: cell }">
          <fieldset v-if="cell.data.Entry && cell.data.Token" class="dx-static-label-border">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <div class="flex items-center">
              <div class="rounded-l-md border bg-primary/70 text-white p-2 min-w-[142px] text-center">伺服器/Stream URL</div>
              <div class="border p-2 w-full">
                {{ `${cell.data.PushUri}` }}
              </div>
              <button class="rounded-r-md border bg-primary/70 text-white p-2 w-16" @click="copyToClipboard(cell.data.PushUri)">複製</button>
            </div>
            <div class="flex items-center mt-1">
              <span class="rounded-l-md border bg-primary/70 text-white p-2 min-w-[142px] text-center">推流碼/Stream Key</span>
              <div class="border p-2 w-full">
                {{ `${cell.data.Entry}` }}
              </div>
              <button class="rounded-r-md border bg-primary/70 text-white p-2 w-16" @click="copyToClipboard(cell.data.Entry)">複製</button>
            </div>
          </fieldset>
        </template>
        <DxColumn data-field="Uri" caption="影片預覽" edit-cell-template="video-player-template" :visible="false" />
        <template #video-player-template="{ data: cell }">
          <fieldset v-if="cell.data.Entry && cell.data.Token" class="dx-static-label-border">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <VideoPlayer ref="player" class="w-full rounded-md" :height="400" :src="`${cell.data.Uri}/playlist.m3u8?t=${$user.Token}`" />
          </fieldset>
        </template>
        <DxColumn data-field="ThumbnailUri" caption="縮圖" edit-cell-template="thumbnail-edit-template" :visible="false" />
        <template #thumbnail-edit-template="{ data: cell }">
          <fieldset class="dx-static-label-border">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <ImageEditor 
              current-path="images"
              selection-mode="single"
              :allowed-file-extensions="['.png', '.jpg', '.svg']"
              :default-uri="'/images/camera.svg'"
              :height="190" 
              :limited-width="200" 
              :limited-height="200"
              :file-system-provider="fileSystemProvider"
              :create="true"
              :copy="true"
              :move="true"
              :remove="true"
              :rename="true"
              :upload="true"
              :download="true"
              :value="cell.value"
              @change="(value) => grid.instance.cellValue(cell.rowIndex, cell.column.dataField, value)"
            />
          </fieldset>
        </template>
        <DxColumn data-field="Title" caption="標題" edit-cell-template="title-edit-template" :visible="false" />
        <template #title-edit-template="{ data: cell }">
          <fieldset class="dx-static-label-border">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <MultilingualTextEditor 
              :culture="culture" 
              :value="cell.data.Title"
              @change="(value: any) => grid.instance.cellValue(cell.rowIndex, cell.column.dataField, value)"
            >
              <template #="{ text, change }">
                <DxTextBox
                  placeholder="請輸入文字"
                  :value="text.Content"
                  @value-changed="(e: any) => change(e.value)"
                />
              </template>
            </MultilingualTextEditor>
          </fieldset>
        </template>
        <DxColumn data-field="Content" caption="內容" edit-cell-template="content-edit-template" :visible="false" />
        <template #content-edit-template="{ data: cell }">
          <fieldset class="dx-static-label-border">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <MultilingualTextEditor :culture="culture" v-model="content">
              <template #default="{ text }">
                <ClassicEditor 
                  :config="contentEditorConfig" 
                  v-model="text.Content"
                />
              </template>
            </MultilingualTextEditor>
          </fieldset>
        </template>
        <DxEditing :allow-adding="true" :allow-updating="true" :allow-deleting="true" :use-icons="true" mode="popup">
          <DxPopup 
            :show-title="true" 
            :minWidth="708" 
            :width="800" 
            :title="$t('app.streaming.live-source.modal.title')" 
            :resizeEnabled="true"
            :full-screen="fullscreenable"
          >
            <DxToolbarItem toolbar="top" location="after">
              <MinimizeIcon v-if="fullscreenable" class="-mr-4 cursor-pointer" @click="fullscreenable=false" />
              <MaximizeIcon v-else class="-mr-4 cursor-pointer" @click="fullscreenable=true" />
            </DxToolbarItem>
            <DxToolbarItem
              widget="dxButton"
              toolbar="bottom"
              location="before"
            >
              <Dropdown class="mr-auto">
                <DropdownToggle class="w-[120px] btn btn-primary">
                  <img width="22" :src="culture.Picture.Uri" />&nbsp;{{ culture.Name}}
                </DropdownToggle>
                <DropdownMenu>
                  <DropdownContent>
                    <DropdownItem v-for="item in cultures" @click="() => culture = item">
                      <img class="outline outline-1 outline-gray-300" width="22" :src="item.Picture.Uri" />&nbsp;{{ item.Name }}
                    </DropdownItem>
                  </DropdownContent>
                </DropdownMenu>
              </Dropdown>
            </DxToolbarItem>
            <DxToolbarItem 
              widget="dxButton" 
              toolbar="bottom" 
              location="after" 
              :options="{ 
                text: $t('DevExtreme.dxDataGrid-editingSaveRowChanges'),
                onClick: () => {
                  let rowIndex = grid.instance.getRowIndexByKey(editingRow.Id);
                  if (rowIndex === -1) rowIndex = 0;
                  grid.instance.cellValue(rowIndex, 'Content', content);
                  grid.instance.saveEditData();
                },
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
          </DxPopup>
          <DxForm label-mode="static" :col-count="1">
            <DxItem :col-count="3" item-type="group">
              <DxItem data-field="ThumbnailUri" />
              <DxItem :col-span="2" :col-count="2" item-type="group">
                <DxItem :col-span="2" data-field="Title" />
                <DxItem :col-span="2" data-field="Entry" />
                <DxItem :col-span="2" data-field="Token" />
                <DxItem data-field="PublisherId" />
                <DxItem data-field="Ordinal" />
              </DxItem>
              <DxItem data-field="Published" :label="{ visible: false }" />
              <DxItem :col-span="2" :col-count="2" item-type="group">
                <DxItem data-field="StartTime" />
                <DxItem data-field="EndTime" />
              </DxItem>
            </DxItem>
            <DxItem data-field="PushUri" :col-count="3" />
            <DxItem data-field="Uri" :col-count="3" />
            <DxItem data-field="Content" :col-count="3" />
          </DxForm>        
        </DxEditing>
      </DxDataGrid>
    </div>
    <!-- END: Content -->
  </div>
</template>

<script lang="ts">
import context, { defineComponent, ISitemapNode, ref } from '@cloudfun/core'

import { Workbook } from 'exceljs';
import { saveAs } from 'file-saver';
import DxTagBox from 'devextreme-vue/tag-box';
import CustomStore from 'devextreme/data/custom_store';
import { exportDataGrid } from 'devextreme/excel_exporter';
import { EditingStartEvent } from 'devextreme/ui/data_grid';
import { DxSwitch } from 'devextreme-vue/switch';
import DxTextBox from 'devextreme-vue/text-box';
import DxTextArea from 'devextreme-vue/text-area';
import {
  DxDataGrid,
  DxStateStoring,
  DxSelection,
  DxEditing,
  DxPopup,
  DxToolbarItem,
  DxForm,
  DxGroupItem,
  DxItem,
  DxExport,
  DxGroupPanel,
  DxColumnChooser,
  DxSearchPanel,
  DxHeaderFilter,
  DxFilterRow,
  DxRowDragging,
  DxPager,
  DxColumn,
  DxLookup,
  DxRequiredRule,
  DxEmailRule,
  DxPatternRule,
  DxCustomRule,
} from 'devextreme-vue/data-grid';

import MultilingualImageEditor from '@cloudfun/multilingual-image-editor'

import MultilingualTextEditor from '@cloudfun/multilingual-text-editor';

import ImageEditor from '@/cloudfun/image-editor.vue';

import UploadAdapterPlugin from '@/global-components/ckeditor/upload-adapter-plugin';

import VideoPlayer from '@/cloudfun/video-player.vue';

export default defineComponent({
  components: {
    DxDataGrid,
    DxStateStoring,
    DxSelection,
    DxEditing,
    DxPopup,
    DxToolbarItem,
    DxForm,
    DxGroupItem,
    DxItem,
    DxExport,
    DxGroupPanel,
    DxColumnChooser,
    DxSearchPanel,
    DxHeaderFilter,
    DxFilterRow,
    DxRowDragging,
    DxPager,
    DxColumn,
    DxLookup,
    DxRequiredRule,
    DxEmailRule,
    DxPatternRule,
    DxCustomRule,
    DxTagBox,
    MultilingualTextEditor,
    MultilingualImageEditor,
    ImageEditor,
    DxSwitch,
    DxTextBox,
    DxTextArea,
    VideoPlayer,
  },
  setup() {
    const contentEditorConfig = {
      extraPlugins: [ UploadAdapterPlugin ],
      removePlugins: ['MediaEmbedToolbar'],
      toolbar: {
        shouldNotGroupWhenFull: true,
        items: [
          "heading", "|",
          // "fontFamily", "fontSize", "fontColor", "fontBackgroundColor",
          "bold", "italic", "underline", 
          // "alignment", "bulletedList", "numberedList", "outdent", "indent", "highlight", 
          "insertTable", "|",
          "link", "blockQuote", "imageInsert", "mediaEmbed", /*"codeBlock",*/ "htmlEmbed", "|",
          "undo", "redo", "sourceEditing"
        ]
      },
      heading: {
        options: [
          { model: "paragraph", title: "Paragraph", class: "ck-heading_paragraph" },
          { model: "heading1", title: "Heading 1", class: "ck-heading_heading1", view: { name: "h1", classes: "font-bold text-xl" } },
          { model: "heading2", title: "Heading 2", class: "ck-heading_heading2", view: { name: "h2", classes: "font-bold text-base" } },
        ]
      }
    }

    return {
      grid: ref<any>({}),
      player: ref<any>({}),
      liveSourceStore: ref<CustomStore>(),
      userStore: ref<CustomStore>(),
      cultures: ref<any>([]),
      culture: ref(),
      content: ref<{ DefaultText: string, Texts: { Culture: string, Content: string }[]}>({ DefaultText: "", Texts: [] }),
      editingRow: ref<any>({ Picture: {} }),
      contentEditorConfig,
      fullscreenable: ref(false),
   };
  },
  async beforeMount() {
    this.liveSourceStore = await this.$model.dispatch('liveSource/getStore');
    this.userStore = await this.$model.dispatch('user/getStore');
  },
  mounted() {
    this.$model.dispatch("culture/query").then(
      payload => { 
        this.cultures = payload;
        this.culture = this.cultures.find((e: any) => e.IsDefault); 
      },
      _failure => this.$send("error", "無法取得文化特性列表"),
    );
  },
  computed: {
    fileSystemProvider() {
      return (limitedWidth?: number, limitedHeight?:number) => `${import.meta.env.VITE_SERVICE_URI}/api/File/Execute?limited=${limitedWidth}x${limitedHeight}`
    }
  },
  methods: {
    onGridAdding(e: any) {
      e.data.Host = import.meta.env.VITE_WOWZA_HOST;
      e.data.Port = import.meta.env.VITE_WOWZA_PORT;
      e.data.Application = import.meta.env.VITE_WOWZA_APPLICATION;
      e.data.Title = { DefaultText: '', Texts: [] }; 
      e.data.Content = { DefaultText: '', Texts: [] }; 
      e.data.Published = false; 
      this.editingRow = e.data;
      this.content = context.utils.clone(e.data.Content, true);
    },
    onGridEditing(e: EditingStartEvent) {
      this.editingRow = e.data;
      this.content = context.utils.clone(e.data.Content, true);
    },
    onGridExporting(e: any) {
      const workbook = new Workbook();
      const worksheet = workbook.addWorksheet('User');
      exportDataGrid({ component: e.component, worksheet, autoFilterEnabled: true }).then(() => {
        workbook.xlsx.writeBuffer().then((buffer) => {
          saveAs(new Blob([buffer], { type: 'application/octet-stream' }), 'media-file.xlsx');
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
        this.$model.dispatch('mediaFile/reorder', { source: source.Id, target: target.Id, isAfter }).then(
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
    pausePlayer() {
      this.player?.player?.pause();
    },
    copyToClipboard(text: string) {
      navigator.clipboard.writeText(text);
      this.$send("info", { subject: "複制成功", content: "已複製至剪貼簿" })
    }
  },
})
</script>
