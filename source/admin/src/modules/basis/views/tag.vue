<template>
  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-12 lg:col-span-2 2xl:col-span-1 sm:hidden flex mt-6">
      <h2 class="intro-y text-lg font-medium mr-auto pl-2">{{($breadcrumb[$breadcrumb.length-1] as ISitemapNode).title}}</h2>
    </div>
    <!-- BEGIN: Content -->
    <div class="intro-y col-span-12 box mt-0 sm:mt-6 p-3">
      <DxDataGrid
        :remote-operations="true"
        :data-source="tagStore"
        :allow-column-resizing="true"
        :show-borders="true"
        :row-alternation-enabled="true"
        @exporting="onExporting"
        @toolbarPreparing="(e: any) => e.toolbarOptions.items.unshift({ location: 'after', widget: 'dxButton', options: { 
          icon: 'refresh', 
          hint: '重置狀態', 
          onClick: function() { 
            e.component.state(null);
            e.component.refresh();
          }
        }})"
      >
        <DxStateStoring :enabled="true" type="localStorage" storage-key="tag-grid" />
        <DxSelection mode="multiple" />
        <DxEditing :allow-adding="true" :allow-updating="true" :allow-deleting="true" :use-icons="true" mode="row" />
        <DxExport :enabled="true" :allow-export-selected-data="true" />
        <DxSearchPanel :visible="true" />
        <DxHeaderFilter :visible="true"/>
        <DxFilterRow :visible="true"/>
        <DxRowDragging
          :allow-reordering="true"
          :on-reorder="onGridReorder"
        />
        <DxColumn data-field="Name" caption="姓名" :allow-sorting="false" />
        <DxColumn data-field="Ordinal" caption="排序" data-type="number" width="100" sort-order="asc" :allow-sorting="false" />
        <DxPager :show-page-size-selector="true" :allowed-page-sizes="[10, 20, 50]" :show-navigation-buttons="true" :show-info="true" />
      </DxDataGrid>
    </div>
    <!-- END: Content -->
  </div>
</template>

<script lang="ts">
import { defineComponent, ISitemapNode, ref } from '@cloudfun/core'

import { Workbook } from 'exceljs';
import { saveAs } from 'file-saver';
import { exportDataGrid } from 'devextreme/excel_exporter';
import CustomStore from 'devextreme/data/custom_store';
import {
  DxDataGrid,
  DxStateStoring,
  DxSelection,
  DxEditing,
  DxExport,
  DxSearchPanel,
  DxHeaderFilter,
  DxFilterRow,
  DxRowDragging,
  DxColumn,
  DxPager,
} from 'devextreme-vue/data-grid';


export default defineComponent({
  components: {
    DxDataGrid,
    DxStateStoring,
    DxSelection,
    DxEditing,
    DxExport,
    DxSearchPanel,
    DxHeaderFilter,
    DxFilterRow,
    DxRowDragging,
    DxColumn,
    DxPager,
  },
  setup () {
    return {
      tagStore: ref<CustomStore>(),
    };
  },
  async beforeMount() {
    this.tagStore = await this.$model.dispatch('tag/getStore');
  },
  methods: {
    onExporting(e: any) {
      const workbook = new Workbook();
      const worksheet = workbook.addWorksheet('Person');
      exportDataGrid({ component: e.component, worksheet, autoFilterEnabled: true }).then(() => {
        workbook.xlsx.writeBuffer().then((buffer) => {
          saveAs(new Blob([buffer], { type: 'application/octet-stream' }), 'person.xlsx');
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
        this.$model.dispatch('tag/reorder', { source: source.Id, target: target.Id, isAfter }).then(
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
  },
})
</script>
