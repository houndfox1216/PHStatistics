<template>
  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-12 lg:col-span-2 2xl:col-span-1 sm:hidden flex mt-6">
      <h2 class="intro-y text-lg font-medium mr-auto pl-2">{{$breadcrumb[$breadcrumb.length-1].title}}</h2>
    </div>
    <!-- BEGIN: Content -->
    <div class="intro-y col-span-12 box p-5 mt-0 sm:mt-6">
      <Stepper 
        ref="stepper"
        v-bind="stepperOptions"
        @next="next"
        @complete="complete"
      >
        <template #step1="{ node, nodes }">
          <DxForm 
            ref="roleForm"
            label-mode="static"
            :form-data="node.data" 
            :col-count="1"
          >
            <DxItem data-field="Name" :label="{ text: '名稱' }">
              <DxRequiredRule />
            </DxItem>
            <DxItem data-field="Description" :label="{ text: '說明' }" />
            <DxItem data-field="PermissionValues" :label="{ text: '權限' }">
              <fieldset class="border rounded p-2">
                <legend class="dx-static-label">權限</legend>
                <CheckboxList
                  :columnCount="4"
                  :items="permissionItems"
                  v-model="node.data.PermissionValues"
                />
              </fieldset>
            </DxItem>
          </DxForm>        
        </template>
        <template #step2="{ node, nodes }">
          <DxForm 
            ref="userForm"
            label-mode="static"
            :form-data="node.data" 
            :col-count="1"
          >
            <DxItem :col-count="3" item-type="group">
              <DxItem>
                <fieldset class="dx-static-label-border">
                  <legend class="dx-static-label">照片</legend>
                  <ImageEditor 
                    current-path="photos"
                    selection-mode="single"
                    :allowed-file-extensions="['.png', '.jpg', '.svg']"
                    :default-uri="'/images/avatar.svg'"
                    :height="160" 
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
                    v-model="node.data.Photo.Uri"
                  />
                </fieldset>
              </DxItem>
              <DxItem :col-span="2" item-type="group">
                <DxItem data-field="Name" :label="{ text: '姓名' }">
                  <DxRequiredRule />
                </DxItem>
                <DxItem 
                  data-field="PersonId" 
                  :label="{ text: '個人資料' }" 
                  editor-type="dxLookup"
                  :editor-options="{
                    dataSource: personStore,
                    searchEnabled: true,
                    searchExpr: 'Name',
                    displayExpr: 'Name',
                    valueExpr: 'Id',
                    dropDownOptions: {
                      showTitle: false,
                      toolbarItems: [],
                      hideOnOutsideClick: true,
                    }
                  }"
                />
                <DxItem data-field="Account" :label="{ text: '帳號' }">
                  <DxRequiredRule />
                </DxItem>
                <DxItem data-field="Password" :label="{ text: '密碼' }">
                  <DxCustomRule :validation-callback="validatePassword" message="須8碼以上含大小寫英文、數字" />
                </DxItem>
              </DxItem>
            </DxItem>
            <DxItem :col-count="3" item-type="group">
              <DxItem 
                data-field="Status" 
                :label="{ text: '狀態' }" 
                data-type="number" 
                editor-type="dxLookup"
                :editor-options="{
                  dataSource: Object.values($enums.UserStatus).map(e => { return { label: e.Name, value: e.Value } }),
                  displayExpr: 'label',
                  valueExpr: 'value',
                  dropDownOptions: {
                    showTitle: false,
                    toolbarItems: [],
                    hideOnOutsideClick: true,
                  }
                }"
              />
              <DxItem data-field="Email" :col-span="2">
                <DxRequiredRule />
                <DxEmailRule/>
              </DxItem>
              <DxItem data-field="RoleIds" :label="{ text: '角色' }" :col-span="3">
                <fieldset class="dx-static-label-border">
                  <legend class="dx-static-label">角色</legend>
                  <DxTagBox
                    :data-source="roleStore"
                    :search-enabled="true"
                    search-expr="Name"
                    display-expr="Name"
                    value-expr="Id"
                    v-model="node.data.RoleIds"
                  />
                </fieldset>
              </DxItem>
            </DxItem>
          </DxForm>        
        </template>
      </Stepper>
    </div>
    <!-- END: Content -->
  </div>
</template>

<script>
import CloudFun, { computed, defineComponent, ref } from '@cloudfun/core';

import "@cloudfun/stepper/dist/stepper.scss";
import Stepper from "@cloudfun/stepper";

import "@cloudfun/checkbox-list/dist/checkbox-list.scss";
import CheckboxList from '@cloudfun/checkbox-list';

import DxTagBox from 'devextreme-vue/tag-box';
import { DxLookup } from 'devextreme-vue/lookup';
import { DxForm, DxItem, DxRequiredRule, DxEmailRule, DxCustomRule } from 'devextreme-vue/form';

import ImageEditor from '@/cloudfun/image-editor.vue';

export default defineComponent({
  components: {
    Stepper,
    CheckboxList,
    DxForm,
    DxItem,
    DxRequiredRule,
    DxEmailRule,
    DxCustomRule,
    DxLookup,
    DxTagBox,
    ImageEditor,
  },
  setup () {
    const model = CloudFun.current?.model

    // #region step 1

    const roleForm = ref();
    const permissionItems = computed(() => {
      const items = []
      for (const permission of Object.values(model?.enums.SystemPermission || {})) {
        if (permission.GroupName != null) {
          let group = items.find(e => e.name === permission.GroupName)
          if (!group) items.push(group = { name: permission.GroupName, items: [], order: 0 })
          group.items.push({ name: permission.Name, value: permission.Value, order: permission.Order })
          if (group.order < permission.Order) group.order = permission.Order
        } else items.push({ name: permission.Name, value: permission.Value, order: permission.Order })
      }
      return items.sort((a, b) => a.order - b.order)
    })

    // #endregion
    // #region step 2

    const userForm = ref();

    // #endregion

    const stepper = ref();
    const stepperOptions = ref({
      nodes: [
        {
          step: 1,
          title: '建立角色',
          data: {},
          template: 'step1',
          skipable: true,
          reset: async (node) => { node.data = {} },
          validate: () => {
            const form = roleForm.value.instance;
            const result = form.validate();
            return result.isValid;
          },
        },
        {
          step: 2,
          title: '建立用戶',
          data: { Status: 0, Online: false, Photo: {} },
          template: 'step2',
          reset: async (node) => { node.data = { Status: 0, Online: false, Photo: {} } },
          validate: () => {
            const form = userForm.value.instance;
            const result = form.validate();
            return result.isValid;
          },
        },
      ],
      backable: true,
      jumpable: true,
    });

    return {
      stepper,
      stepperOptions,
      roleForm,
      permissionItems,
      userForm,
      personStore: ref([]),
      roleStore: ref([]),
    };
  },
  async beforeMount() {
    this.personStore = await this.$model.dispatch('person/getStore');
    this.roleStore = await this.$model.dispatch('role/getStore');
  },
  computed: {
    fileSystemProvider() {
      return (limitedWidth, limitedHeight) => `${import.meta.env.VITE_SERVICE_URI}/api/File/Execute?limited=${limitedWidth}x${limitedHeight}`
    }
  },
  methods: {
    next(params, callback) {
      const from = params.from;
      const to = params.to;
      switch (from.step) {
        case 1:
        if (from.data.Id) return;
          this.$model.dispatch('role/insert', from.data).then(
            payload => {
              from.data = payload;
              if (!to.data) to.data = {};
              if (!to.data.RoleIds) to.data.RoleIds = [];
              to.data.RoleIds.push(payload.Id);
              callback();
            },
            failure => {
              this.$send('error', {
                subject: this.$tc('model.error.insert'),
                content: failure.message || failure
              });
            }
          )
          break;
      }
    },
    complete(nodes, callback) {
      const node = nodes[1];
      if (node.data.Id) return;
      this.$model.dispatch('user/insert', node.data).then(
        payload => {
          node.data = payload,
          callback();
        },
        failure => {
          this.$send('error', {
            subject: this.$tc('model.error.insert'),
            content: failure.message || failure
          });
        }
      );
    },
    generateStatusList() {
      Object.values(this.$enums.UserStatus).map(e => { return { label: e.Name, value: e.Value } })
    },
    validatePassword(e) {
      if (!e.value && this.nodes[1].Id) return true; // optional
      const regex = new RegExp("^((?=.{8,}$)(?=.*\\d)(?=.*[a-z])(?=.*[A-Z]).*|(?=.{8,}$)(?=.*\\d)(?=.*[a-zA-Z])(?=.*[!\\u0022#$%&'()*+,./:;<=>?@[\\]\\^_`{|}~-]).*)");
      return regex.test(e.value);
    },
  }
});
</script>
