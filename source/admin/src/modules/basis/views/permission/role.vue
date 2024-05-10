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
        :data-source="roleStore"
        :remote-operations="true"
        :allow-column-resizing="true"
        :allow-column-reordering="true"
        :row-alternation-enabled="true"
        @init-new-row="onGridAdding"
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
        <DxStateStoring :enabled="true" type="localStorage" storage-key="rule-grid" />
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
        <DxColumn data-field="Description" caption="說明" />
        <DxColumn data-field="PermissionValues" caption="權限" edit-cell-template="permissions-edit-template" :visible="false" />
        <template #permissions-edit-template="{ data: cell }">
          <fieldset class="border rounded p-2">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <CheckboxList
              :columnCount="4"
              :items="permissionItems"
              :value="cell.value"
              @change="(value) => grid.instance.cellValue(cell.rowIndex, cell.column.dataField, value)"
            />
          </fieldset>
        </template>
        <DxEditing :allow-adding="true" :allow-updating="true" :allow-deleting="true" :use-icons="true" mode="popup">
          <DxPopup 
            :show-title="true" 
            :width="700" 
            :height="544" 
            :minWidth="578" 
            :title="$t('app.basis.permission.role.modal.title')" 
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
          <DxForm label-mode="floating" :col-count="1">
            <DxItem data-field="Name" />
            <DxItem data-field="Description" />
            <DxItem data-field="PermissionValues" />
          </DxForm>        
        </DxEditing>
      </DxDataGrid>
    </div>
    <!-- END: Content -->
  </div>
</template>

<script lang="ts">
import context, { defineComponent, ISitemapNode, ref, computed } from '@cloudfun/core'

import { Workbook } from 'exceljs';
import { saveAs } from 'file-saver';
import { DxTextArea  } from 'devextreme-vue';
import CustomStore from 'devextreme/data/custom_store';
import { exportDataGrid } from 'devextreme/excel_exporter';
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

import "@cloudfun/checkbox-list/dist/checkbox-list.scss";
import CheckboxList from '@cloudfun/checkbox-list';

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
    DxTextArea,
    CheckboxList
  },
  setup () {
    const model = context.root!.model;
    const permissionItems = computed(() => {
      const items: any[] = []
      for (const permission of Object.values(model?.enums.SystemPermission || {})) {
        if (permission.GroupName != null) {
          let group = items.find(e => e.name === permission.GroupName)
          if (!group) items.push(group = { name: permission.GroupName, items: [], order: 0 })
          group.items.push({ name: permission.Name, value: permission.Value, order: permission.Order })
          if (group.order < permission.Order) group.order = permission.Order
        } else items.push({ name: permission.Name, value: permission.Value, order: permission.Order })
      }
      return items.sort((a: any, b: any) => a.order - b.order)
    });

    return {
      grid: ref<any>({}),
      roleStore: ref<CustomStore>(),
      permissionItems,
      fullscreenable: ref(false),
    };
  },
  async beforeMount() {
    this.roleStore = await this.$model.dispatch('role/getStore');
  },
  methods: {
    onGridAdding(e: any) {
      e.data.Photo = { };
      e.data.Address = { };
    },
    onGridExporting(e: any) {
      const workbook = new Workbook();
      const worksheet = workbook.addWorksheet('Person');
      exportDataGrid({ component: e.component, worksheet, autoFilterEnabled: true }).then(() => {
        workbook.xlsx.writeBuffer().then((buffer) => {
          saveAs(new Blob([buffer], { type: 'application/octet-stream' }), 'person.xlsx');
        });
      });
      e.cancel = true;
    },
  }
})
</script>
