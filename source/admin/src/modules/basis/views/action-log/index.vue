<template>
  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-12 lg:col-span-2 2xl:col-span-1 sm:hidden flex mt-6">
      <h2 class="intro-y text-lg font-medium mr-auto pl-2">{{($breadcrumb[$breadcrumb.length-1] as ISitemapNode).title}}</h2>
    </div>
    <!-- BEGIN: Content -->
    <div class="intro-y col-span-12 box mt-0 sm:mt-6 p-3">
      <DxDataGrid
        :remote-operations="true"
        :data-source="actionLogStore"
        :allow-column-reordering="true"
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
        <DxStateStoring :enabled="true" type="localStorage" storage-key="data-grid-storage" />
        <DxSelection mode="multiple"/>
        <DxExport :enabled="true" :allow-export-selected-data="true" />
        <DxGroupPanel :visible="true"/>
        <DxColumnChooser :enabled="true" />
        <DxSearchPanel :visible="true" />
        <DxHeaderFilter :visible="true"/>
        <DxColumn data-field="CreatedTime" data-type="datetime" caption="時間" :width="150" :fixed="true" />
        <DxColumn data-field="UserTypeName" caption="登入類型" />
        <DxColumn data-field="UserDataTypeName" caption="用戶類型" />
        <DxColumn data-field="UserName" caption="用戶名稱" />
        <DxColumn data-field="ActionName" caption="操作名稱" />
        <DxColumn data-field="EntityTypeName" caption="資料類型" />
        <DxColumn data-field="EntityName" caption="資料名稱" />
        <DxMasterDetail :enabled="true" template="detail" />
        <template #detail="{ data }"><Detail :id="data.key" /></template>
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
  DxExport,
  DxGroupPanel,
  DxColumnChooser,
  DxSearchPanel,
  DxHeaderFilter,
  DxColumn,
  DxMasterDetail,
  DxPager,
} from 'devextreme-vue/data-grid';

import Detail from './detail.vue';

export default defineComponent({
  components: {
    DxDataGrid,
    DxStateStoring,
    DxSelection,
    DxExport,
    DxGroupPanel,
    DxColumnChooser,
    DxSearchPanel,
    DxHeaderFilter,
    DxColumn,
    DxMasterDetail,
    DxPager,
    Detail,
  },
  setup () {
    return {
      actionLogStore: ref<CustomStore>(),
    };
  },
  async beforeMount() {
    this.actionLogStore = await this.$model.dispatch('actionLog/getStore');
  },
  methods: {
    onExporting(e: any) {
      const workbook = new Workbook();
      const worksheet = workbook.addWorksheet('Action Log');
      exportDataGrid({ component: e.component, worksheet, autoFilterEnabled: true }).then(() => {
        workbook.xlsx.writeBuffer().then((buffer) => {
          saveAs(new Blob([buffer], { type: 'application/octet-stream' }), 'action-log.xlsx');
        });
      });
      e.cancel = true;
    },  }
})
</script>
