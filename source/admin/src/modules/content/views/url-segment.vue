<template>
  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-12 lg:col-span-2 2xl:col-span-1 sm:hidden flex mt-6">
      <h2 class="intro-y text-lg font-medium mr-auto pl-2">{{($breadcrumb[$breadcrumb.length-1] as ISitemapNode).title}}</h2>
    </div>
    <!-- BEGIN: Content -->
    <div class="intro-y col-span-12 box mt-0 sm:mt-6 p-3">
      <DxTreeList
        ref="tree"
        :data-source="urlSegmentStore"
        :root-value="-1"
        :show-row-lines="true"
        :show-borders="true"
        :column-auto-width="true"
        key-expr="Id"
        parent-id-expr="ParentId"
        @init-new-row="onTreeAdding"
        @editing-start="onTreeEditing"
        @toolbarPreparing="(e: any) => e.toolbarOptions.items.unshift({ location: 'after', widget: 'dxButton', options: { 
          icon: 'refresh', 
          hint: '重置狀態', 
          onClick: function() { 
            e.component.state(null);
            e.component.refresh();
          }
        }})"
      >
        <DxStateStoring :enabled="true" type="localStorage" storage-key="url-segment-tree" />
        <DxSelection mode="multiple" />
        <DxColumnChooser :enabled="true" />
        <DxSearchPanel :visible="true" />
        <DxHeaderFilter :visible="true"/>
        <DxFilterRow :visible="true"/>
        <DxScrolling mode="standard" />
        <DxPaging :enabled="true" :page-size="10" />  
        <DxPager :show-page-size-selector="true" :allowed-page-sizes="[10, 20, 50]" :show-navigation-buttons="true" :show-info="true" />
        <DxRowDragging
          :on-reorder="onTreeReorder"
          :allow-drop-inside-item="true"
          :allow-reordering="true"
          :show-drag-icons="true"
        />
        <DxColumn data-field="Name" caption="名稱" :allow-sorting="false" />
        <DxColumn data-field="Title.DefaultText" caption="標題" :allow-sorting="false" />
        <DxColumn data-field="PageId" caption="網頁" :allow-sorting="false">
          <DxLookup :data-source="pageStore" :search-enabled="true" search-expr="Name" display-expr="Name" value-expr="Id" />
        </DxColumn>
        <DxColumn data-field="LinkUrl" caption="鏈結網址" :allow-sorting="false" />
        <DxColumn data-field="Ordinal" caption="排序值" data-type="number" :width="100" sort-order="asc" :allow-sorting="false" />
        <DxColumn data-field="Title" caption="標題" edit-cell-template="title-edit-template" :visible="false" />
        <template #title-edit-template="{ data: cell }">
          <fieldset class="dx-static-label-border">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <MultilingualTextEditor 
              :culture="culture" 
              :value="cell.data.Title"
              @change="(value: any) => tree.instance.cellValue(cell.rowIndex, cell.column.dataField, value)"
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
        <DxEditing :allow-adding="true" :allow-updating="true" :allow-deleting="true" :use-icons="true" mode="popup">
          <DxPopup 
            :show-title="true" 
            :minWidth="300" 
            :width="500" 
            :height="380" 
            :title="$t('app.content.url-segment.modal.title')" 
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
                  let rowIndex = tree.instance.getRowIndexByKey(editingRow.Id);
                  if (rowIndex === -1) rowIndex = 0;
                  tree.instance.cellValue(rowIndex, 'Content', content);
                  tree.instance.saveEditData();
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
                  if (tree.instance.hasEditData()) tree.instance.refresh(true);
                  tree.instance.cancelEditData(); 
                },
              }"
            />
          </DxPopup>
          <DxForm label-mode="static" :col-count="2">
            <DxItem data-field="Name" />
            <DxItem data-field="Ordinal" />
            <DxItem data-field="Title" :col-span="2" />
            <DxItem data-field="PageId" :col-span="2"/>
            <DxItem data-field="LinkUrl" :col-span="2"/>
          </DxForm>        
        </DxEditing>
      </DxTreeList>
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
  DxTreeList, 
  DxColumn, 
  DxFilterRow, 
  DxStateStoring, 
  DxColumnChooser, 
  DxSearchPanel,
  DxHeaderFilter,
  DxScrolling,
  DxPaging,
  DxPager,
  DxEditing,
  DxSelection,
  DxPopup,
  DxToolbarItem,
  DxForm,
  DxItem,
  DxLookup,
  DxRequiredRule,
  DxEmailRule,
  DxPatternRule,
  DxCustomRule,
  DxRowDragging,
} from 'devextreme-vue/tree-list';

import MultilingualImageEditor from '@cloudfun/multilingual-image-editor'

import MultilingualTextEditor from '@cloudfun/multilingual-text-editor';

import ImageEditor from '@/cloudfun/image-editor.vue';

import UploadAdapterPlugin from '@/global-components/ckeditor/upload-adapter-plugin';

export default defineComponent({
  components: {
    DxTreeList,
    DxStateStoring,
    DxSelection,
    DxEditing,
    DxPopup,
    DxToolbarItem,
    DxForm,
    DxItem,
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
    DxScrolling,
    DxPaging,
    DxRowDragging,
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
      tree: ref<any>({}),
      urlSegmentStore: ref<CustomStore>(),
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
    this.urlSegmentStore = await this.$model.dispatch('urlSegment/getStore');
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
    onTreeAdding(e: any) {
      if (e.data.ParentId === -1) delete e.data.ParentId;
      e.data.Title = { DefaultText: '', Texts: [] }; 
      this.editingRow = e.data;
      this.content = context.utils.clone(e.data.Content, true);
    },
    onTreeEditing(e: EditingStartEvent) {
      this.editingRow = e.data;
      this.content = context.utils.clone(e.data.Content, true);
    },
    onTreeReorder(e: any) {
      if (e.toIndex === -1) e.cancel = true;
      else {
        const source = e.itemData;
        const visibleRows = e.component.getVisibleRows();
        const targetIndex = e.dropInsideItem || e.fromIndex > e.toIndex ? e.toIndex : e.toIndex + 1;
        const target = visibleRows[targetIndex < visibleRows.length ? targetIndex : visibleRows.length-1].data;
        const isAfter = !e.dropInsideItem && targetIndex >= visibleRows.length;
        if (e.dropInsideItem) {
          source.ParentId = target.Id;
          this.$model.dispatch('urlSegment/update', source).then(
            () => e.component.refresh(),
            failure => {
              this.$send('error', { 
                subject: this.$tc("model.error.update"), 
                content: failure.message 
              });
              e.cancel = true;
            }
          );
        } else {
          this.$model.dispatch('urlSegment/reorder', { source: source.Id, target: target.Id, isAfter }).then(
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
      }
    }
  },
})
</script>
