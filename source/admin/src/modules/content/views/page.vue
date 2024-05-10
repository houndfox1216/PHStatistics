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
        :data-source="pageStore"
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
        <DxStateStoring :enabled="true" type="localStorage" storage-key="news-grid" />
        <DxSelection mode="multiple" />
        <DxExport :enabled="true" :allow-export-selected-data="true" />
        <DxGroupPanel :visible="true"/>
        <DxColumnChooser :enabled="true" />
        <DxSearchPanel :visible="true" />
        <DxHeaderFilter :visible="true"/>
        <DxFilterRow :visible="true"/>
        <DxPager :show-page-size-selector="true" :allowed-page-sizes="[10, 20, 50]" :show-navigation-buttons="true" :show-info="true" />
        <DxColumn data-field="Name" caption="名稱">
          <DxRequiredRule />
        </DxColumn>
        <DxColumn data-field="MetaTitle" caption="SEO標題" />
        <DxColumn data-field="MetaKeywords" caption="SEO關鍵字" :visible="false" />
        <DxColumn data-field="MetaDescription" caption="SEO描述" :visible="false" editor-type="dxTextArea" :editor-options="{ autoResizeEnabled: true }" />
        <DxColumn data-field="CanonicalUrl" caption="正規URL" :visible="false" />
        <DxColumn data-field="StartDate" caption="開始日期" data-type="date" :visible="false" :editor-options="{ displayFormat: 'yyyy/MM/dd' }" />
        <DxColumn data-field="EndDate" caption="結束日期" data-type="date" :visible="false" :editor-options="{ displayFormat: 'yyyy/MM/dd' }" />
        <DxColumn data-field="Published" caption="已發佈" data-type="boolean">
          <DxLookup :data-source="[{ label: '是', value: true }, { label: '否', value: false }]" display-expr="label" value-expr="value" />
        </DxColumn>
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
            :title="$t('app.content.news.modal.title')" 
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
            <DxItem data-field="Name" />
            <DxItem data-field="MetaTitle" />
            <DxItem data-field="MetaKeywords" />
            <DxItem data-field="MetaDescription" />
            <DxItem data-field="CanonicalUrl" />
            <DxItem :col-count="3" item-type="group">
              <DxItem data-field="Published" :label="{ visible: false }" />
              <DxItem data-field="StartDate" />
              <DxItem data-field="EndDate" />
            </DxItem>
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
      pageStore: ref<CustomStore>(),
      cultures: ref<any>([]),
      culture: ref(),
      content: ref<{ DefaultText: string, Texts: { Culture: string, Content: string }[]}>({ DefaultText: "", Texts: [] }),
      editingRow: ref<any>({ Picture: {} }),
      contentEditorConfig,
      fullscreenable: ref(false),
   };
  },
  async beforeMount() {
    this.pageStore = await this.$model.dispatch('page/getStore');
  },
  mounted() {
    this.$model.dispatch("culture/query").then(
      payload => { 
        this.cultures = payload;
        this.culture = this.cultures.find((e: any) => e.IsDefault); 
      },
      _failure => this.$send("error", "無法取得文化特性列表"),
    )
  },
  computed: {
    fileSystemProvider() {
      return (limitedWidth?: number, limitedHeight?:number) => `${import.meta.env.VITE_SERVICE_URI}/api/File/Execute?limited=${limitedWidth}x${limitedHeight}`
    }
  },
  methods: {
    onGridAdding(e: any) {
      e.data.Picture = { DefaultImageUri: '', Images: [] };
      e.data.Title = { DefaultText: '', Texts: [] }; 
      e.data.Introduction = { DefaultText: '', Texts: [] }; 
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
          saveAs(new Blob([buffer], { type: 'application/octet-stream' }), 'user.xlsx');
        });
      });
      e.cancel = true;
    },
  },
})
</script>
