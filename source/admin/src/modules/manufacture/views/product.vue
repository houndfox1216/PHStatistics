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
        :data-source="productStore"
        :remote-operations="true"
        :allow-column-resizing="true"
        :allow-column-reordering="true"
        :row-alternation-enabled="true"
        :repaint-changes-only="true"
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
        <DxStateStoring :enabled="true" type="localStorage" storage-key="product-grid" />
        <DxSelection mode="multiple" />
        <DxExport :enabled="true" :allow-export-selected-data="true" />
        <DxGroupPanel :visible="true"/>
        <DxColumnChooser :enabled="true" />
        <DxSearchPanel :visible="true" />
        <DxHeaderFilter :visible="true"/>
        <DxFilterRow :visible="true"/>
        <DxPager :show-page-size-selector="true" :allowed-page-sizes="[10, 20, 50]" :show-navigation-buttons="true" :show-info="true" />
        <DxColumn data-field="Number" caption="編號"  :editor-options="{ placeholder: '未輸入時由系統產生', maxLength: 32 }" />
        <DxColumn data-field="Name" caption="名稱" :editor-options="{ placeholder: '輸入文字…', maxLength: 32 }">
          <DxRequiredRule />
        </DxColumn>
        <DxColumn data-field="Base.Name" caption="基礎商品" />
        <DxColumn data-field="Picture.Uri" caption="圖片" edit-cell-template="picture-edit-template" :visible="false" :allow-hiding="false" />
        <template #picture-edit-template="{ data: cell }">
          <fieldset class="dx-static-label-border">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <ImageEditor 
              current-path="images"
              selection-mode="single"
              :allowed-file-extensions="['.png', '.jpg', '.svg']"
              :default-uri="'/images/camera.svg'"
              :height="210" 
              :limited-width="300" 
              :limited-height="300"
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
        <DxColumn data-field="BaseId" caption="基礎商品" :visible="false">
          <DxLookup :data-source="productStore" :search-enabled="true" search-expr="Name" display-expr="Name" value-expr="Id" />
        </DxColumn>
        <DxColumn data-field="CategoryIds" caption="類別" edit-cell-template="categories-edit-template" :visible="false">
          <DxRequiredRule />
        </DxColumn>
        <template #categories-edit-template="{ data: cell }">
          <fieldset class="dx-static-label-border">
            <legend class="dx-static-label">
              <span>{{ cell.column.caption }}</span>
              <span v-show="cell.item.isRequired" class="text-red-700 ml-1">*</span>
            </legend>
            <DxTagBox
              :data-source="categoryStore"
              :search-enabled="true"
              search-expr="Name"
              display-expr="Name"
              value-expr="Id"
              :value="cell.value"
              @value-changed="(e: any) => grid.instance.cellValue(cell.rowIndex, cell.column.dataField, e.value)"
            />
          </fieldset>
        </template>
        <DxColumn data-field="TagIds" caption="標籤" edit-cell-template="tags-edit-template" :visible="false" />
        <template #tags-edit-template="{ data: cell }">
          <fieldset class="dx-static-label-border">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <DxTagBox
              :data-source="tagStore"
              :search-enabled="true"
              search-expr="Name"
              display-expr="Name"
              value-expr="Id"
              :value="cell.value"
              @value-changed="(e: any) => grid.instance.cellValue(cell.rowIndex, cell.column.dataField, e.value)"
            />
          </fieldset>
        </template>
        <DxColumn 
          data-field="Mixed" 
          caption="組合商品" 
          :width="120" 
          :editor-options="{
            onItemClick: (e: any) => onMixedChanged(e)
          }"
        >
          <DxLookup :data-source="[{ label: '是', value: true }, { label: '否', value: false } ]" display-expr="label" value-expr="value" />
        </DxColumn>
        <DxColumn data-field="ModelNo" caption="型號" :editor-options="{ placeholder: '輸入文字…', maxLength: 16 }" :visible="false" />
        <DxColumn data-field="PackedContents" caption="內容物" edit-cell-template="contents-edit-template" :visible="false" />
        <template #contents-edit-template="{ data: cell }">
          <fieldset v-if="mixed" class="dx-static-label-border">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <DxDataGrid v-if="cell.data.Id"
              ref="contentGrid"
              :show-borders="true"
              :data-source="packedContentStore"
              :remote-operations="true"
              :allow-column-resizing="true"
              :row-alternation-enabled="true"
              @init-new-row="(e: any) => e.data.ProductId = cell.data.Id"
            >
              <DxColumn data-field="ContentId" caption="商品">
                <DxRequiredRule />
                <DxLookup :data-source="productStore" :search-enabled="true" search-expr="Name" display-expr="Name" value-expr="Id" />
              </DxColumn>
              <DxColumn data-field="Quantity" caption="數量" />
              <DxEditing :allow-adding="true" :allow-updating="true" :allow-deleting="true" :use-icons="true" />
            </DxDataGrid>
            <div v-else class="flex justify-center items-center py-3">
              新增後方可建立內容物
            </div>
          </fieldset>
        </template>
        <DxColumn data-field="ProductAttributeValues" caption="屬性" edit-cell-template="attributes-edit-template" :visible="false" />
        <template #attributes-edit-template="{ data: cell }">
          <fieldset class="dx-static-label-border">
            <legend class="dx-static-label flex items-center">屬性</legend>
            <AttributeValueEditor 
              :attributes="attributes" 
              :value="attributeValues" 
              @change="(value: any) => {
                attributeValuesChanged = true;
                attributeValues = value;
              }"
            />
          </fieldset>
        </template>
        <DxColumn data-field="Album.Pictures" caption="圖片集" edit-cell-template="album-edit-template" :visible="false" />
        <template #album-edit-template="{ data: cell }">
          <fieldset class="dx-static-label-border">
            <legend class="dx-static-label flex items-center">圖片集</legend>
            <GalleryEditor v-if="cell.data.Id"
              ref="albumEditor"
              :navigation="true"
              :width="albumEditorWidth"
              :value="cell.data.Album?.Pictures ?? []"
              @add="(image: any, callback: any) => { image.AlbumId = cell.data.Album.Id; callback(); }"
              @edit="(image: any, callback: any) => { image.AlbumId = cell.data.Album.Id; callback(); }"
              @save="onAlbumEditorSave"
              @remove="onAlbumEditorRemove"
              @change="(value: any) => grid.instance.cellValue(cell.rowIndex, cell.column.dataField, value)"
            >
              <template #default="{ data, save, visible, setVisible }">
                <DxPopup
                  :title="`${data?.Id ? '編輯' : '新增'}相片`"
                  :drag-enabled="true"
                  :resizeEnabled="true"
                  :show-close-button="true"
                  :show-title="true"
                  :width="600"
                  :height="430"
                  container="body"
                  :visible="visible"
                  @update:visible="(value: boolean) => setVisible(value)"
                >
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
                      current-path="albums"
                      selection-mode="single"
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
                      @change="(value: any) => data.Uri = value"
                    />
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
            <div v-else class="flex justify-center items-center py-3">
              新增後方可建立圖片集
            </div>
          </fieldset>
        </template>
        <DxEditing 
          mode="popup"
          :allow-adding="true" 
          :allow-updating="true" 
          :allow-deleting="true" 
          :use-icons="true" 
        >
          <DxGridPopup 
            :title="$t('app.manufacture.product.modal.title')"
            :show-title="true" 
            :width="800" 
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
                onClick: () => {
                  let rowIndex = grid.instance.getRowIndexByKey(editingRow.Id);
                  if (rowIndex === -1) rowIndex = 0;
                  if (attributeValuesChanged) grid.instance.cellValue(rowIndex, 'ProductAttributeValues', attributeValues);
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
          </DxGridPopup>
          <DxGridForm label-mode="static" :col-count="1">
            <DxItem :col-count="3" item-type="group">
              <DxItem data-field="Picture.Uri" />
              <DxItem :col-span="2" :col-count="2" item-type="group">
                <DxItem data-field="Number" />
                <DxItem data-field="ModelNo"/>
                <DxItem data-field="Name" :col-span="2" />
                <DxItem data-field="CategoryIds" :col-span="2" />
                <DxItem data-field="TagIds" :col-span="2" />
              </DxItem>
            </DxItem>
            <DxItem :col-count="3" item-type="group">
              <DxItem data-field="Mixed" :label="{ visible: false }"/>
              <DxItem data-field="BaseId" :col-span="2" />
            </DxItem>
            <DxItem data-field="PackedContents" :col-span="3" />
            <DxItem data-field="ProductAttributeValues" :col-span="3" />
            <DxItem data-field="Album.Pictures" :col-span="3" />
          </DxGridForm>
        </DxEditing>
      </DxDataGrid>
    </div>
    <!-- END: Content -->
  </div>
</template>

<script lang="ts">
import context, { defineComponent, ISitemapNode, ref, Condition, Operator, Sorting, SortOrder } from '@cloudfun/core'

import { Workbook } from 'exceljs';
import { saveAs } from 'file-saver';
import { DxTextArea } from 'devextreme-vue';
import DxTagBox from 'devextreme-vue/tag-box';
import CustomStore from 'devextreme/data/custom_store';
import { exportDataGrid } from 'devextreme/excel_exporter';
import { EditingStartEvent, InitNewRowEvent } from 'devextreme/ui/data_grid';
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
  DxColumn,
  DxLookup,
  DxRequiredRule,
  DxEmailRule,
  DxPatternRule,
  DxCustomRule,
} from 'devextreme-vue/data-grid';

import AttributeValueEditor from '@/cloudfun/attribute-value-editor.vue'

import GalleryEditor from '@/cloudfun/gallery-editor.vue'
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
    DxColumn,
    DxLookup,
    DxRequiredRule,
    DxEmailRule,
    DxPatternRule,
    DxCustomRule,
    DxTextArea,
    DxTagBox,
    DxSwitch,
    AttributeValueEditor,
    DxPopup,
    DxToolbarItem,
    GalleryEditor,
    ImageEditor,
    DxForm,
    DxFormItem,
  },
  setup () {
    return {
      grid: ref<any>({}),
      productStore: ref<CustomStore>(),
      packedContentStore: ref<CustomStore>(),
      categoryStore: ref<CustomStore>(),
      tagStore: ref<CustomStore>(),
      editingRow: ref<any>({ Picture: {} }),
      mixed: ref(false),
      attributes: ref<any[]>([]),
      attributeValuesChanged: ref(false),
      attributeValues: ref<any[]>(),
      albumEditor: ref<any>({}),
      albumEditorWidth: ref<string>(),
      pictureForm: ref<any>({}),
      fullscreenable: ref(false),
    };
  },
  async beforeMount() {
    this.productStore = await this.$model.dispatch('product/getStore');
    this.packedContentStore = await this.$model.dispatch('packedContent/getStore');
    this.categoryStore = await this.$model.dispatch('category/getStore');
    this.tagStore = await this.$model.dispatch('tag/getStore');
    await this.loadAttributes();
  },
  computed: {
    fileSystemProvider() {
      return (limitedWidth?: number, limitedHeight?:number) => `${import.meta.env.VITE_SERVICE_URI}/api/File/Execute?limited=${limitedWidth}x${limitedHeight}`
    }
  },
  methods: {
    onGridAdding(e: InitNewRowEvent) {
      e.data.Picture = { };
      this.editingRow = e.data;
      this.attributeValuesChanged = false;
      this.attributeValues = undefined;
      this.mixed = false;
    },
    async onGridEditing(e: EditingStartEvent) {
      this.editingRow = e.data;
      this.attributeValuesChanged = false;
      this.attributeValues = e.data.ProductAttributeValues;
      if (this.editingRow.Id && this.editingRow.Mixed) {
        this.mixed = true;
        var condition = new Condition("ProductId", Operator.Equal, this.editingRow.Id);
        this.packedContentStore = await this.$model.dispatch('packedContent/getStore', () => { return { condition } });
      }
    },
    onGridExporting(e: any) {
      const workbook = new Workbook();
      const worksheet = workbook.addWorksheet('Product');
      exportDataGrid({ component: e.component, worksheet, autoFilterEnabled: true }).then(() => {
        workbook.xlsx.writeBuffer().then((buffer) => {
          saveAs(new Blob([buffer], { type: 'application/octet-stream' }), 'product.xlsx');
        });
      });
      e.cancel = true;
    },
    async onGridModalContentReady(e: any) {
      if (e.component._$content.length > 0) this.albumEditorWidth = `${e.component._$content[0].clientWidth-100}px`;
    },
    onGridModalResize(size: { width: number }) {
      this.albumEditorWidth = `${size.width - 100}px`;
    },
    onImageEditorUpdated(value: string) { 
      let rowIndex = this.grid.instance.getRowIndexByKey(this.editingRow.Id);
      if (rowIndex === -1) rowIndex = 0;
      this.grid.instance.cellValue(rowIndex, "Picture.Uri", value);
    },
    onCategoryIdsChanged(e: any) { 
      const values = e.value;
      let rowIndex = this.grid.instance.getRowIndexByKey(this.editingRow.Id);
      if (rowIndex === -1) rowIndex = 0;
      this.grid.instance.cellValue(rowIndex, "CategoryIds", values);
    },
    onTagIdsChanged(e: any) { 
      const values = e.value;
      let rowIndex = this.grid.instance.getRowIndexByKey(this.editingRow.Id);
      if (rowIndex === -1) rowIndex = 0;
      this.grid.instance.cellValue(rowIndex, "TagIds", values);
    },
    onMixedChanged(e: any) {
      this.mixed = e.itemData.value;
    },
    async loadAttributes() {
      if (this.attributes.length === 0) {
        const attributes = await this.$model.dispatch("attribute/query", { sortings: [new Sorting("Ordinal", SortOrder.Ascending)] });
        for (let attribute of attributes) {
          if (attribute.Selectable) {
            attribute = await this.$model.dispatch("attribute/find", attribute.Id);
            attribute.Values.sort((a: any, b: any) => a.DecimalValue - b.DecimalValue);
          }
          this.attributes.push(attribute);
        }
      }
    },
    async onAlbumEditorRemove (image: any, callback: any) {
      await this.$model.dispatch('picture/delete', image.Id);
      callback();
    },
    async onAlbumEditorSave (params: any, callback: any) {
      await this.$model.dispatch('picture/save', params).then(
        () => {
          this.$model.dispatch('album/find', this.editingRow.Album.Id).then(
            (entity) => {
              entity.Pictures.forEach((picture: any) => {
                delete picture.$id;
                delete picture.Album;
              });
              this.albumEditor.reload(entity.Pictures.sort((a: any, b: any) => a.Ordinal - b.Ordinal))
            },
            reason => { context.send('error', { subject: '重載失敗', content: reason }) }
          )
        },
        reason => { context.send('error', { subject: '保存失敗', content: reason }) }
      );
      callback();
    },
  }
})
</script>
