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
        :data-source="userStore"
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
        <DxStateStoring :enabled="true" type="localStorage" storage-key="user-grid" />
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
        <DxColumn data-field="Account" caption="帳號">
          <DxRequiredRule />
        </DxColumn>
        <DxColumn data-field="Email" caption="Email">
          <DxRequiredRule />
          <DxEmailRule/>
        </DxColumn>
        <DxColumn data-field="PasswordExpirationPolicy" caption="密碼策略" data-type="number" :editor-options="{ showClearButton: true, placeholder: '無' }">
          <DxLookup 
            :data-source="[
              { label: '30天', value: 30 },
              { label: '60天', value: 60 },
              { label: '90天', value: 90 },
            ]" 
            display-expr="label" 
            value-expr="value" 
          />
        </DxColumn>
        <DxColumn data-field="PasswordChangedTime" caption="密碼變更時間" data-type="datetime" :visible="false" :editor-options="{ displayFormat: 'yyyy/MM/dd HH:mm:ss' }" />
        <DxColumn data-field="Status" caption="狀態" data-type="number">
          <DxLookup :data-source="Object.values($enums.UserStatus).map(e => { return { label: e.Name, value: e.Value } })" display-expr="label" value-expr="value" />
        </DxColumn>
        <DxColumn 
          data-field="Online"
          caption="登入狀態" 
          data-type="boolean" 
          width="134" 
          editor-type="dxSwitch"
          calculate-sort-value="LastVisitedTime"
          :allow-filtering="false"
          :editor-options="{ width: 'auto', switchedOnText: '上線', switchedOffText: '離線' }" 
        />
        <DxColumn data-field="Photo.Uri" caption="照片" edit-cell-template="photo-edit-template" :visible="false" />
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
        <DxColumn data-field="PersonId" caption="個人資料" :visible="false">
          <DxLookup :data-source="personStore" :search-enabled="true" search-expr="Name" display-expr="Name" value-expr="Id" />
        </DxColumn>
        <DxColumn data-field="Password" caption="密碼" :editor-options="{ mode: 'password' }" :visible="false">    
          <DxCustomRule :validation-callback="validatePassword" message="須8碼以上含大小寫英文、數字" />
        </DxColumn>
        <DxColumn data-field="RoleIds" caption="角色" edit-cell-template="roles-edit-template" :visible="false" />
        <template #roles-edit-template="{ data: cell }">
          <fieldset class="dx-static-label-border">
            <legend class="dx-static-label">{{ cell.column.caption }}</legend>
            <DxTagBox
              :data-source="roleStore"
              :search-enabled="true"
              search-expr="Name"
              display-expr="Name"
              value-expr="Id"
              :value="cell.value"
              @value-changed="(e: any) => grid.instance.cellValue(cell.rowIndex, cell.column.dataField, e.value)"
            />
          </fieldset>
        </template>
        <DxColumn data-field="LastVisitedTime" caption="訪問時間" data-type="datetime" :visible="false" :editor-options="{ displayFormat: 'yyyy/MM/dd HH:mm:ss' }" />
        <DxColumn data-field="LoginTime" caption="登入時間" data-type="datetime" :visible="false" :editor-options="{ displayFormat: 'yyyy/MM/dd HH:mm:ss' }" />
        <DxColumn data-field="LogoutTime" caption="登出時間" data-type="datetime" :visible="false" :editor-options="{ displayFormat: 'yyyy/MM/dd HH:mm:ss' }" />
        <DxColumn data-field="Remark" caption="備註" :visible="false" editor-type="dxTextArea" />
        <DxEditing :allow-adding="true" :allow-updating="true" :allow-deleting="true" :use-icons="true" mode="popup">
          <DxPopup 
            :show-title="true" 
            :width="700" 
            :height="525" 
            :minWidth="578" 
            :title="$t('app.basis.permission.user.modal.title')" 
            :resizeEnabled="true" 
            :full-screen="fullscreenable"
            @showing="onPopupShowing"
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
                <DxItem data-field="PersonId" />
                <DxItem data-field="Account" />
                <DxItem data-field="Password" />
              </DxItem>
            </DxItem>
            <DxItem :col-count="3" item-type="group">
              <DxItem data-field="Status"/>
              <DxItem data-field="Email" :col-span="2" />
              <DxItem data-field="RoleIds" :col-span="3" />
              <DxItem data-field="Online" css-class="my-auto !w-full" :label="{ visible: false }" :disabled="true" />
              <DxItem data-field="LoginTime" :col-span="2" :disabled="true" />
              <DxItem data-field="PasswordExpirationPolicy"/>
              <DxItem data-field="LastVisitedTime" :col-span="2" :disabled="true"/>
              <DxItem data-field="PasswordChangedTime" :disabled="true" />
              <DxItem data-field="LogoutTime" :col-span="2" :disabled="true" />
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
import DxTagBox from 'devextreme-vue/tag-box';
import CustomStore from 'devextreme/data/custom_store';
import { exportDataGrid } from 'devextreme/excel_exporter';
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
  DxColumn,
  DxLookup,
  DxRequiredRule,
  DxEmailRule,
  DxPatternRule,
  DxCustomRule,
} from 'devextreme-vue/data-grid';

import ImageEditor from '@/cloudfun/image-editor.vue';
import model from '@/models';
import { EditingStartEvent } from 'devextreme/ui/data_grid';
import { ShowingEvent } from 'devextreme/ui/popup';

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
    ImageEditor,
    DxSwitch,
  },
  setup() {
    return {
      grid: ref<any>({}),
      popup: ref<any>({}),
      userStore: ref<CustomStore>(),
      personStore: ref<CustomStore>(),
      roleStore: ref<CustomStore>(),
      fullscreenable: ref(false),
      popupVisible: ref(false),
   };
  },
  async beforeMount() {
    this.userStore = await this.$model.dispatch('user/getStore');
    this.personStore = await this.$model.dispatch('person/getStore');
    this.roleStore = await this.$model.dispatch('role/getStore');
  },
  computed: {
    fileSystemProvider() {
      return (limitedWidth?: number, limitedHeight?:number) => `${import.meta.env.VITE_SERVICE_URI}/api/File/Execute?limited=${limitedWidth}x${limitedHeight}`
    }
  },
  methods: {
    onGridAdding(e: any) {
      e.data.Status = 0;
      e.data.Photo = { };
      e.data.Address = { };
      e.data.PasswordExpirationPolicy = 90;
    },
    async onGridEditing(e: EditingStartEvent) {
      this.popupVisible = false;
      this.grid.instance.beginCustomLoading("Loading")
      const configuration = await model.dispatch('configuration/read')
      e.data.Email = configuration.AdminTitle
      this.grid.instance.endCustomLoading()
      this.popupVisible = true;
      this.popup.show()
    },
    onPopupShowing(e: ShowingEvent) {
      this.popup = e.component
      if (!this.popupVisible) e.cancel = true
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
    validatePassword(e: any) {
      if (!e.valuee && e.data.Id) return true; // optional
      const regex = new RegExp("^((?=.{8,}$)(?=.*\\d)(?=.*[a-z])(?=.*[A-Z]).*|(?=.{8,}$)(?=.*\\d)(?=.*[a-zA-Z])(?=.*[!\\u0022#$%&'()*+,./:;<=>?@[\\]\\^_`{|}~-]).*)");
      return regex.test(e.value);
    },
  },
})
</script>
