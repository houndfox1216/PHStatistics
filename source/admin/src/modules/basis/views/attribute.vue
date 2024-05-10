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
        :data-source="attributeStore"
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
        <DxStateStoring :enabled="true" type="localStorage" storage-key="attribute-grid" />
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
        <DxColumn data-field="Code" caption="代碼" :editor-options="{ maxLength: 32 }">
          <DxRequiredRule />
        </DxColumn>
        <DxColumn data-field="Name" caption="名稱" :editor-options="{ maxLength: 64 }">
          <DxRequiredRule />
        </DxColumn>
        <DxColumn data-field="Required" caption="必填">
          <DxLookup :data-source="[{ label: '是', value: true }, { label: '否', value: false } ]" display-expr="label" value-expr="value" />
        </DxColumn>
        <DxColumn data-field="Selectable" caption="選擇型" :editor-options="{ onSelectionChanged: (e: any) => valueGridVisible = e.selectedItem.value }">
          <DxLookup :data-source="[{ label: '是', value: true }, { label: '否', value: false }]" display-expr="label" value-expr="value" />
        </DxColumn>
        <DxColumn data-field="Multiple" caption="可複選">
          <DxLookup :data-source="[{ label: '是', value: true }, { label: '否', value: false } ]" display-expr="label" value-expr="value" />
        </DxColumn>
        <DxColumn data-field="Remark" caption="備註" editor-type="dxTextArea" :editor-options="{ maxLength: 512 }" />
        <DxColumn data-field="Ordinal" caption="排序值" data-type="number" :width="100" sort-order="asc" />
        <DxColumn data-field="Values" caption="選項" edit-cell-template="values-edit-template" :visible="false" />
        <template #values-edit-template="{ data: cell }">
          <fieldset v-show="valueGridVisible" class="dx-static-label-border">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <DxDataGrid 
              v-if="cell.data.Id"
              ref="valueGrid"
              :show-borders="true"
              :data-source="attributeValueStore"
              :remote-operations="true"
              :allow-column-resizing="true"
              :row-alternation-enabled="true"
              @init-new-row="(e: any) => e.data.AttributeId = cell.data.Id"
            >
              <DxColumn data-field="TextValue" caption="文字(顯示或查詢用)" />
              <DxColumn data-field="DecimalValue" caption="數值(比較或排序用)" />
              <DxColumn data-field="Value" caption="值(程式邏輯用)" />
              <DxEditing :allow-adding="true" :allow-updating="true" :allow-deleting="true" :use-icons="true" />
            </DxDataGrid>
            <div v-else class="flex justify-center items-center py-20">
              新增選擇型屬性後方可建立選項
            </div>
          </fieldset>
        </template>
        <DxEditing :allow-adding="true" :allow-updating="true" :allow-deleting="true" :use-icons="true" mode="popup">
          <DxPopup 
            :show-title="true" 
            :width="700" 
            :height="610" 
            :minWidth="578" 
            :title="$t('app.basis.attribute.modal.title')" 
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
          </DxPopup>
          <DxForm label-mode="static" :col-count="4">
            <DxItem data-field="Code" :col-span="4" />
            <DxItem data-field="Name" :col-span="4" />
            <DxItem data-field="Required" :label="{ visible: false }" css-class="my-auto" />
            <DxItem data-field="Selectable" :label="{ visible: false }" css-class="my-auto" />
            <DxItem data-field="Multiple" :label="{ visible: false }" css-class="my-auto" />
            <DxItem data-field="Ordinal" />
            <DxItem data-field="Remark" :col-span="4" />
            <DxItem data-field="Values" :col-span="4" />
          </DxForm>
        </DxEditing>
      </DxDataGrid>
    </div>
    <!-- END: Content -->
  </div>
</template>

<script lang="ts">
import { defineComponent, ISitemapNode, ref, Condition, Operator } from '@cloudfun/core'

import { Workbook } from 'exceljs';
import { saveAs } from 'file-saver';
import { DxTextArea  } from 'devextreme-vue';
import CustomStore from 'devextreme/data/custom_store';
import { exportDataGrid } from 'devextreme/excel_exporter';
import { EditingStartEvent } from 'devextreme/ui/data_grid';
import { DxSwitch } from 'devextreme-vue/switch';
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
  DxRowDragging,
  DxColumn,
  DxLookup,
  DxRequiredRule,
  DxEmailRule,
  DxPatternRule,
  DxCustomRule,
} from 'devextreme-vue/data-grid';

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
    DxRowDragging,
    DxColumn,
    DxLookup,
    DxRequiredRule,
    DxEmailRule,
    DxPatternRule,
    DxCustomRule,
    DxTextArea,
    DxSwitch,
  },
  setup () {
    return {
      grid: ref<any>({}),
      attributeStore: ref<CustomStore>(),
      attributeValueStore: ref<CustomStore>(),
      valueGridVisible: ref(false),
      fullscreenable: ref(false),
    };
  },
  async beforeMount() {
    this.attributeStore = await this.$model.dispatch('attribute/getStore');
    this.attributeValueStore = await this.$model.dispatch('attributeValue/getStore');
  },
  methods: {
    async onGridAdding(e: any) {
      this.valueGridVisible = false;
    },
    async onGridEditing(e: EditingStartEvent) {
      this.valueGridVisible = e.data.Selectable;
      var condition = new Condition("AttributeId", Operator.Equal, e.data.Id ?? 0);
      this.attributeValueStore = await this.$model.dispatch('attributeValue/getStore', () => { return { condition } });
    },
    onGridExporting(e: any) {
      const workbook = new Workbook();
      const worksheet = workbook.addWorksheet('Attribute');
      exportDataGrid({ component: e.component, worksheet, autoFilterEnabled: true }).then(() => {
        workbook.xlsx.writeBuffer().then((buffer) => {
          saveAs(new Blob([buffer], { type: 'application/octet-stream' }), 'attribute.xlsx');
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
        this.$model.dispatch('attribute/reorder', { source: source.Id, target: target.Id, isAfter }).then(
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
  }
})
</script>
