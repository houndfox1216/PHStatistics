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
        :data-source="personStore"
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
        <DxStateStoring :enabled="true" type="localStorage" storage-key="person-grid" />
        <DxSelection mode="multiple" />
        <DxExport :enabled="true" :allow-export-selected-data="true" />
        <DxGroupPanel :visible="true"/>
        <DxColumnChooser :enabled="true" />
        <DxSearchPanel :visible="true" />
        <DxHeaderFilter :visible="true"/>
        <DxFilterRow :visible="true"/>
        <DxPager :show-page-size-selector="true" :allowed-page-sizes="[10, 20, 50]" :show-navigation-buttons="true" :show-info="true" />
        <DxColumn data-field="Name" caption="姓名">
          <DxRequiredRule />
        </DxColumn>
        <DxColumn data-field="Sex" caption="性別" data-type="number">
          <DxLookup :data-source="Object.values($enums.Sex).map(e => { return { label: e.Name, value: e.Value } })" display-expr="label" value-expr="value" />
        </DxColumn>
        <DxColumn data-field="Nickname" caption="暱稱" />
        <DxColumn data-field="Email" caption="Email">
          <DxEmailRule/>
        </DxColumn>
        <DxColumn data-field="PersonalId" caption="護照編號" :visible="false">    
          <DxPatternRule :pattern="new RegExp('[0-9a-zA-z]{8,}')" message="須8碼以上僅含英數字" />
        </DxColumn>
        <DxColumn data-field="Photo.Uri" caption="照片" edit-cell-template="photo-edit-template" :visible="false" :allow-hiding="false" />
        <template #photo-edit-template="{ data: cell }">
          <fieldset class="dx-static-label-border">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <ImageEditor 
              current-path="photos"
              selection-mode="single"
              :allowed-file-extensions="['.png', '.jpg', '.svg']"
              :default-uri="'/images/avatar.svg'"
              :height="174" 
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
        <DxColumn data-field="MobilePhone" caption="行動電話" :visible="false">
          <DxCustomRule :validation-callback="validatePhoneNumber" message="電話號碼格式不正確" />
        </DxColumn>
        <DxColumn data-field="BirthDate" caption="出生日期" data-type="date" :visible="false" />
        <DxColumn data-field="Phone" caption="電話號碼" :visible="false">
          <DxCustomRule :validation-callback="validatePhoneNumber" message="電話號碼格式不正確" />
        </DxColumn>
        <DxColumn data-field="Fax" caption="傳真號碼" :visible="false">
          <DxCustomRule :validation-callback="validatePhoneNumber" message="電話號碼格式不正確" />
        </DxColumn>
        <DxColumn data-field="Address.Line" caption="地址" :visible="false" />
        <DxColumn data-field="Remark" caption="備註" :visible="false" editor-type="dxTextArea" />
        <DxEditing :allow-adding="true" :allow-updating="true" :allow-deleting="true" :use-icons="true" mode="popup">
          <DxPopup 
            :show-title="true" 
            :width="700" 
            :height="544" 
            :minWidth="578" 
            :title="$t('app.basis.person.modal.title')" 
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
          <DxForm label-mode="static" :col-count="1">
            <DxItem :col-count="3" item-type="group">
              <DxItem data-field="Photo.Uri" />
              <DxItem :col-span="2" item-type="group">
                <DxItem data-field="Name" />
                <DxItem data-field="Nickname" />
                <DxItem data-field="PersonalId"/>
                <DxItem data-field="MobilePhone"/>
              </DxItem>
            </DxItem>
            <DxItem :col-count="3" item-type="group">
              <DxItem data-field="Sex"/>
              <DxItem :col-span="2" data-field="BirthDate"/>
              <DxItem :col-span="3" data-field="Email" />
              <DxItem :col-span="3" data-field="Address.Line" />
              <DxItem :col-span="3" data-field="Remark" />
            </DxItem>
          </DxForm>        
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

import ImageEditor from '@/cloudfun/image-editor.vue';

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
    ImageEditor,
  },
  setup() {
    return {
      grid: ref<any>({}),
      personStore: ref<CustomStore>(),
      fullscreenable: ref(false),
   };
  },
  async beforeMount() {
    this.personStore = await this.$model.dispatch('person/getStore');
  },
  computed: {
    fileSystemProvider() {
      return (limitedWidth?: number, limitedHeight?:number) => `${import.meta.env.VITE_SERVICE_URI}/api/File/Execute?limited=${limitedWidth}x${limitedHeight}`
    }
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
    validatePhoneNumber(e: any) {
      if (!e.value) return true; // optional
      return this.$utils.validator.validatePhoneNumber(e.value) === undefined;
    },
  },
})
</script>
