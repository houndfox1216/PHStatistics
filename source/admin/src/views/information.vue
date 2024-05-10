<template>
  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-12 lg:col-span-2 2xl:col-span-1 sm:hidden flex mt-6">
      <h2 class="intro-y text-lg font-medium mr-auto pl-2">{{($breadcrumb[$breadcrumb.length-1] as ISitemapNode).title}}</h2>
    </div>
    <!-- BEGIN: Content -->
    <div class="intro-y col-span-12 box p-5 mt-0 sm:mt-6">
      <p>Service Assembly Version: {{ assemblyVersion }}</p>
      <p>Service Application Version: {{ serviceAppVersion }}</p>
      <p>Application(vue) Version: {{ appVersion }}</p>
    </div>
    <div class="intro-y col-span-12 box p-5 mt-0 sm:mt-6">
        <textarea class="w-full p-2 rounded-md border" placeholder="請輸入設定值" v-model="value" />
        <button class="w-full p-2 rounded-md border bg-primary/70 text-white" @click="encrypt">加密</button>
        <div v-if="result" class="w-full p-2 rounded-md border mt-2 break-words">{{ result }}</div>
    </div>
    <!-- END: Content -->
  </div>
</template>

<script lang="ts">
import CloudFun, { defineComponent, ISitemapNode, ref } from '@cloudfun/core';

export default defineComponent({
  setup () {
    const model = CloudFun.root!.model;

    return {
      assemblyVersion: ref(""),
      serviceAppVersion: ref(""),
      appVersion: ref(""),
      value: ref(""),
      result: ref<string>(),
    };
  },
  created() {
    this.$model.clients.unauthorized.get("/System/Information").then(
      (success: any) => {
        this.assemblyVersion = success.payload.assemblyVersion;
        this.serviceAppVersion = success.payload.appVersion;
        this.appVersion = process.env.npm_package_version ?? "";
      }
    )
  },
  methods: {
    encrypt() {
      this.$model.clients.unauthorized.get("/System/EncryptConfigurationValue?value=" + this.value).then(
        (success: any) => this.result = success.payload
      )
    }
  }
})
</script>
