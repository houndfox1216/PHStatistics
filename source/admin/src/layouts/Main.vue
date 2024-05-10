<template>
  <TopMenu v-if="$model.getters['midone/layoutMode'] === 'top'" />
  <SimpleMenu v-else-if="$model.getters['midone/layoutMode'] === 'simple'" />
  <SideMenu v-else />
</template>

<script setup lang="ts">
import SideMenu from "@/layouts/side-menu/Main.vue";
import SimpleMenu from "@/layouts/simple-menu/Main.vue";
import TopMenu from "@/layouts/top-menu/Main.vue";
</script>