<template>
  <div class="intro-y flex items-center mt-8 sm:hidden">
    <h2 class="text-lg font-medium mr-auto">{{$breadcrumb[$breadcrumb.length-1].title}}</h2>
  </div>
  <!-- BEGIN: Display Information -->
  <div class="intro-y box mt-8 sm:mt-5">
    <div
      class="flex items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
    >
      <h2 class="font-medium text-base mr-auto">{{ $t("app.profile.information.information")}} </h2>
    </div>
    <div class="p-5">
      <DxForm 
        ref="userForm"
        label-mode="static"
        :form-data="user" 
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
                :height="144" 
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
                v-model="user.Photo.Uri"
              />
            </fieldset>
          </DxItem>
          <DxItem :col-span="2" item-type="group">
            <DxItem data-field="Name" :label="{ text: '姓名' }" :is-required="true" />
            <DxItem data-field="Email" :is-required="true">
              <DxEmailRule/>
            </DxItem>
            <DxItem data-field="RoleIds" :label="{ text: '角色' }">
              <fieldset class="dx-static-label-border">
                <legend class="dx-static-label">角色</legend>
                <DxTagBox
                  :data-source="roleStore"
                  :search-enabled="true"
                  search-expr="Name"
                  display-expr="Name"
                  value-expr="Id"
                  v-model="user.RoleIds"
                />
              </fieldset>
            </DxItem>
          </DxItem>
        </DxItem>
      </DxForm>        
      <div class="flex justify-end mt-4">
        <button type="button" class="btn btn-primary w-20" @click="save">
          {{ $t("button.save") }}
        </button>
      </div>
    </div>
  </div>
  <!-- END: Display Information -->
  <!-- BEGIN: Personal Information -->
  <div class="intro-y box mt-5">
    <div
      class="flex items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
    >
      <h2 class="font-medium text-base mr-auto">{{ $t("app.profile.information.personal-information")}}</h2>
    </div>
    <div class="p-5">
      <DxForm 
        ref="personForm"
        label-mode="static"
        :read-only="person.DataMode !== 0"
        :form-data="person" 
        :col-count="1"
      >
        <DxItem :col-count="3" item-type="group">
          <DxItem>
            <fieldset class="dx-static-label-border" :read-only="person.DataMode !== 0">
              <legend class="dx-static-label">照片</legend>
              <ImageEditor 
                :disabled="person.DataMode !== 0"
                current-path="photos"
                selection-mode="single"
                :allowed-file-extensions="['.png', '.jpg', '.svg']"
                :default-uri="'/images/avatar.svg'"
                :height="144" 
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
                v-model="person.Photo.Uri"
              />
            </fieldset>
          </DxItem>
          <DxItem :col-span="2" item-type="group">
            <DxItem data-field="Name" :label="{ text: '姓名' }">
              <DxRequiredRule />
            </DxItem>
            <DxItem data-field="Nickname" :label="{ text: '暱稱' }" />
            <DxItem data-field="PersonalId" :label="{ text: '護照編號' }">
              <DxCustomRule :validation-callback="validatePhoneNumber" message="電話號碼格式不正確" />
            </DxItem>
            <DxItem data-field="MobilePhone" :label="{ text: '行動電話' }">
              <DxCustomRule :validation-callback="validatePhoneNumber" message="電話號碼格式不正確" />
            </DxItem>
          </DxItem>
        </DxItem>
        <DxItem :col-count="3" item-type="group">
          <DxItem 
            data-field="Sex" 
            editor-type="dxLookup"
            :label="{ text: '性別' }"
            :editor-options="{
              dataSource: Object.values($enums.Sex).map(e => { return { label: e.Name, value: e.Value } }),
              displayExpr: 'label',
              valueExpr: 'value',
              value: person.Sex,
              onValueChanged: e => person.Sex = e.value,
            }"
          />
          <DxItem data-field="BirthDate" :label="{ text: '出生日期' }" editor-type="dxDateBox" :col-span="2" />
          <DxItem data-field="Email" :col-span="3" />
          <DxItem data-field="Address.Line" :label="{ text: '地址' }" :col-span="3" />
          <DxItem data-field="Remark" :label="{ text: '備註' }" :col-span="3" />
        </DxItem>
      </DxForm>        
      <div v-if="person && person.DataMode === 0" class="flex justify-end mt-4">
        <button type="button" class="btn btn-primary w-20" @click="savePerson()">
          {{ $t("button.save") }}
        </button>
      </div>
    </div>
  </div>
  <!-- END: Personal Information -->
</template>

<script>
import context, { defineComponent, ref } from '@cloudfun/core'

import DxTagBox from 'devextreme-vue/tag-box';
import { DxLookup } from 'devextreme-vue/lookup';
import { DxForm, DxItem, DxRequiredRule, DxEmailRule, DxCustomRule } from 'devextreme-vue/form';

import ImageEditor from '@/cloudfun/image-editor.vue';

export default defineComponent({
  components: {
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
    return { 
      user: ref({ Photo: {} }),
      person: ref({ Photo: {} }),
      userForm: ref({}),
      personForm: ref({}),
      roleStore: ref([]),
    };
  },
  async beforeMount() {
    this.roleStore = await this.$model.dispatch('role/getStore');
  },
 async created() {
    this.user = await this.$model.dispatch("user/find", context.user.Id);
    this.person = await this.$model.dispatch("person/find", this.user.PersonId);
  },
  computed: {
    fileSystemProvider() {
      return (limitedWidth, limitedHeight) => `${import.meta.env.VITE_SERVICE_URI}/api/File/Execute?limited=${limitedWidth}x${limitedHeight}`
    }
  },
  methods: {
    save() {
      if (this.userForm.instance.validate()) {
        this.$model.dispatch("user/update", this.user).then(
          () => this.$send("info", "變更資料成功"),
          failure => this.$send("error", failure.message || failure)
        );
      }
    },
    savePerson() {
      if (this.personForm.instance.validate()) {
        this.$model.dispatch("person/update", this.person).then(
          () => this.$send("info", "變更資料成功"),
          failure => this.$send("error", failure.message || failure)
        );
      }
    },
    validatePhoneNumber(e) {
      if (!e.value) return true; // optional
      return this.$utils.validator.validatePhoneNumber(e.value) === undefined;
    },
  },
});
</script>