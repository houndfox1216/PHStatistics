<template>
  <div class="py-2">
    <MobileMenu />
    <div class="flex">
      <!-- BEGIN: Side Menu -->
      <nav class="side-nav" v-if="$model.getters['midone/activeMenu']">
        <router-link to="/" tag="a" class="intro-x flex items-center pl-5 pt-4" :title="$model.state.configuration.value.AdminTitle || 'CloudFun Admin'">
          <img class="w-6" src="@/assets/images/logo.svg" />
          <span class="hidden xl:block text-white text-lg ml-3"> {{$model.state.configuration.value.AdminTitle || 'CloudFun Admin'}} </span>
        </router-link>
        <div class="side-nav__devider my-6"></div>
        <ul>
          <!-- BEGIN: First Child -->
          <template v-for="(menu, menuKey) in formattedMenu">
            <li v-if="typeof menu == 'string'" :key="`devider-${menuKey}`" class="side-nav__devider my-6 flex items-center">
              <span v-if="menu !== 'devider'" class="w-full rounded-lg bg-white/40 text-xs text-slate-200 text-center">{{ menu }}</span>
            </li>
            <li v-else :key="`menu-${menuKey}`">
              <SideMenuTooltip
                tag="a"
                :content="$t(menu.title)"
                :href="
                  menu.subNodes || !menu.to
                    ? 'javascript:;'
                    : menu.to
                "
                class="side-menu"
                :class="{
                  'side-menu--active': menu.active,
                  'side-menu--open': menu.activeDropdown,
                }"
                @click="linkTo(menu, $router, $event)"
              >
                <div class="side-menu__icon">
                  <ActivityIcon v-if="!menu.icon" class="m-auto" />
                  <img v-else-if="menu.icon.includes('/')" :src="menu.icon" class="m-auto" />
                  <FontAwesome v-else-if="menu.icon.startsWith('fa-')" class="w-6 h-6 m-auto" :icon="menu.icon.substr(3)" />
                  <FontAwesome v-else-if="menu.icon.startsWith('fas-')" class="w-6 h-6 m-auto" type="fas" :icon="menu.icon.substr(4)" />
                  <FontAwesome v-else-if="menu.icon.startsWith('far-')" class="w-6 h-6 m-auto" type="far" :icon="menu.icon.substr(4)" />
                  <component v-else :is="menu.icon" class="m-auto" />
                </div>
                <div class="side-menu__title">
                  {{ $t(menu.title) }}
                  <div
                    v-if="menu.subNodes"
                    class="side-menu__sub-icon"
                    :class="{ 'transform rotate-180': menu.activeDropdown }"
                  >
                    <ChevronDownIcon />
                  </div>
                </div>
              </SideMenuTooltip>
              <!-- BEGIN: Second Child -->
              <transition @enter="enter" @leave="leave">
                <ul class="mb-1" v-if="menu.subNodes && menu.activeDropdown">
                  <li
                    v-for="(subMenu, subMenuKey) in menu.subNodes.filter(e => typeof e !== 'string')"
                    :key="subMenuKey"
                  >
                    <SideMenuTooltip
                      tag="a"
                      :content="$t(subMenu.title)"
                      :href="
                        subMenu.subNodes || !subMenu.to
                          ? 'javascript:;'
                          : subMenu.to
                      "
                      class="side-menu"
                      :class="{ 'side-menu--active': subMenu.active }"
                      @click="linkTo(subMenu, $router, $event)"
                    >
                      <div class="side-menu__icon">
                        <ActivityIcon v-if="!subMenu.icon" class="m-auto" />
                        <img v-else-if="subMenu.icon.includes('/')" :src="subMenu.icon" class="m-auto" />
                        <FontAwesome v-else-if="subMenu.icon.startsWith('fa-')" class="w-6 h-6 m-auto" :icon="subMenu.icon.substr(3)" />
                        <FontAwesome v-else-if="subMenu.icon.startsWith('fas-')" class="w-6 h-6 m-auto" type="fas" :icon="subMenu.icon.substr(4)" />
                        <FontAwesome v-else-if="subMenu.icon.startsWith('far-')" class="w-6 h-6 m-auto" type="far" :icon="subMenu.icon.substr(4)" />
                        <component v-else :is="subMenu.icon" class="m-auto" />
                      </div>
                      <div class="side-menu__title">
                        {{ $t(subMenu.title) }}
                        <div
                          v-if="subMenu.subNodes"
                          class="side-menu__sub-icon"
                          :class="{
                            'transform rotate-180': subMenu.activeDropdown,
                          }"
                        >
                          <ChevronDownIcon />
                        </div>
                      </div>
                    </SideMenuTooltip>
                    <!-- BEGIN: Third Child -->
                    <transition @enter="enter" @leave="leave">
                      <ul v-if="subMenu.subNodes && subMenu.activeDropdown">
                        <li
                          v-for="(lastSubMenu, lastSubMenuKey) in subMenu.subNodes.filter(e => typeof e !== 'string')"
                          :key="lastSubMenuKey"
                        >
                          <SideMenuTooltip
                            tag="a"
                            :content="lastSubMenu.title"
                            :href="
                              lastSubMenu.subNodes || !lastSubMenu.to
                                ? 'javascript:;'
                                : lastSubMenu.to
                            "
                            class="side-menu"
                            :class="{ 'side-menu--active': lastSubMenu.active }"
                            @click="linkTo(lastSubMenu, $router, $event)"
                          >
                            <div class="side-menu__icon">
                              <ZapIcon v-if="!lastSubMenu.icon" class="m-auto" />
                              <img v-else-if="lastSubMenu.icon.includes('/')" :src="lastSubMenu.icon" class="m-auto" />
                              <FontAwesome v-else-if="lastSubMenu.icon.startsWith('fa-')" class="w-6 h-6 m-auto" :icon="lastSubMenu.icon.substr(3)" />
                              <FontAwesome v-else-if="lastSubMenu.icon.startsWith('fas-')" class="w-6 h-6 m-auto" type="fas" :icon="lastSubMenu.icon.substr(4)" />
                              <FontAwesome v-else-if="lastSubMenu.icon.startsWith('far-')" class="w-6 h-6 m-auto" type="far" :icon="lastSubMenu.icon.substr(4)" />
                              <component v-else :is="lastSubMenu.icon" class="m-auto" />
                            </div>
                            <div class="side-menu__title">
                              {{ $t(lastSubMenu.title) }}
                            </div>
                          </SideMenuTooltip>
                        </li>
                      </ul>
                    </transition>
                    <!-- END: Third Child -->
                  </li>
                </ul>
              </transition>
              <!-- END: Second Child -->
            </li>
          </template>
          <!-- END: First Child -->
        </ul>
      </nav>
      <!-- END: Side Menu -->
      <!-- BEGIN: Content -->
      <div class="content">
        <TopBar />
        <router-view />
      </div>
      <!-- END: Content -->
    </div>
  </div>
</template>

<script setup>
import context, { computed, onMounted, ref, watch } from "@cloudfun/core";
import { helper as $h } from "@/utils/helper";
import TopBar from "@/components/top-bar/Main.vue";
import MobileMenu from "@/components/mobile-menu/Main.vue";
import SideMenuTooltip from "@/components/side-menu-tooltip/Main.vue";
import { linkTo, enter, leave } from "./index";
import dom from "@left4code/tw-starter/dist/js/dom";

const formattedMenu = ref([]);
const sideMenu = computed(() => context.current.menu);

watch(
  computed(() => context.route.fullPath),
  () => {
    formattedMenu.value = $h.toRaw(sideMenu.value);
  }
);

onMounted(() => {
  dom("body").removeClass("error-page").removeClass("login").addClass("main");
  formattedMenu.value = $h.toRaw(sideMenu.value);
});
</script>
