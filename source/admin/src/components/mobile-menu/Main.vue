<template>
  <!-- BEGIN: Mobile Menu -->
  <div class="mobile-menu md:hidden">
    <div class="mobile-menu-bar">
      <a href="" class="flex mr-auto" :title="$model.state.configuration.value.AdminTitle || 'CloudFun Admin'">
        <img
          class="w-6"
          src="@/assets/images/logo.svg"
        />
        <span class="text-white text-lg ml-3">{{$model.state.configuration.value.AdminTitle || 'CloudFun Admin'}}</span>
      </a>
      <a href="javascript:;" id="mobile-menu-toggler">
        <BarChart2Icon
          class="w-8 h-8 text-white transform -rotate-90"
          @click="toggleMobileMenu"
        />
      </a>
    </div>
    <transition @enter="enter" @leave="leave">
      <ul
        v-if="activeMobileMenu"
        class="border-t border-white/[0.08] py-5 hidden"
      >
        <!-- BEGIN: First Child -->
        <template v-for="(menu, menuKey) in formattedMenu">
          <li v-if="typeof menu == 'string'" :key="`devider-${menuKey}`" class="side-nav__devider my-6 flex items-center">
            <span v-if="menu !== 'devider'" class="w-full rounded-lg bg-white/40 text-xs text-slate-200 text-center mx-3">{{ menu }}</span>
          </li>
          <li v-else :key="`menu-${menuKey}`">
            <a
              href="javascript:;"
              class="menu"
              :class="{
                'menu--active': menu.active,
                'menu--open': menu.activeDropdown,
              }"
              @click="linkTo(menu, $router)"
            >
              <div class="menu__icon">
                <FontAwesome v-if="menu.icon.startsWith('fa-')" class="w-6 h-6 m-auto" :icon="menu.icon.substr(3)" />
                <FontAwesome v-else-if="menu.icon.startsWith('fas-')" class="w-6 h-6 m-auto" type="fas" :icon="menu.icon.substr(4)" />
                <FontAwesome v-else-if="menu.icon.startsWith('far-')" class="w-6 h-6 m-auto" type="far" :icon="menu.icon.substr(4)" />
                <img v-else-if="menu.icon.includes('/')" :src="menu.icon" class="m-auto" />
                <component v-else :is="menu.icon" class="m-auto" />
              </div>
              <div class="menu__title">
                {{ $t(menu.title) }}
                <div
                  v-if="menu.subNodes"
                  class="menu__sub-icon"
                  :class="{ 'transform rotate-180': menu.activeDropdown }"
                >
                  <ChevronDownIcon />
                </div>
              </div>
            </a>
            <!-- BEGIN: Second Child -->
            <transition @enter="enter" @leave="leave">
              <ul v-if="menu.subNodes && menu.activeDropdown">
                <li
                  v-for="(subMenu, subMenuKey) in menu.subNodes"
                  :key="subMenuKey"
                >
                  <a
                    href="javascript:;"
                    class="menu"
                    :class="{ 'menu--active': subMenu.active }"
                    @click="linkTo(subMenu, $router)"
                  >
                    <div class="menu__icon">
                      <FontAwesome v-if="subMenu.icon.startsWith('fa-')" class="w-6 h-6 m-auto" :icon="subMenu.icon.substr(3)" />
                      <FontAwesome v-else-if="subMenu.icon.startsWith('fas-')" class="w-6 h-6 m-auto" type="fas" :icon="subMenu.icon.substr(4)" />
                      <FontAwesome v-else-if="subMenu.icon.startsWith('far-')" class="w-6 h-6 m-auto" type="far" :icon="subMenu.icon.substr(4)" />
                      <img v-else-if="subMenu.icon.includes('/')" :src="subMenu.icon" class="m-auto" />
                      <component v-else-if="subMenu.icon" :is="subMenu.icon" class="m-auto" />
                      <ActivityIcon v-else class="m-auto" />
                    </div>
                    <div class="menu__title">
                      {{ $t(subMenu.title) }}
                      <div
                        v-if="subMenu.subNodes"
                        class="menu__sub-icon"
                        :class="{
                          'transform rotate-180': subMenu.activeDropdown,
                        }"
                      >
                        <ChevronDownIcon />
                      </div>
                    </div>
                  </a>
                  <!-- BEGIN: Third Child -->
                  <transition @enter="enter" @leave="leave">
                    <ul v-if="subMenu.subNodes && subMenu.activeDropdown">
                      <li
                        v-for="(lastSubMenu, lastSubMenuKey) in subMenu.subNodes"
                        :key="lastSubMenuKey"
                      >
                        <a
                          href="javascript:;"
                          class="menu"
                          :class="{ 'menu--active': lastSubMenu.active }"
                          @click="linkTo(lastSubMenu, $router)"
                        >
                          <div class="menu__icon">
                            <FontAwesome v-if="lastSubMenu.icon.startsWith('fa-')" class="w-6 h-6 m-auto" :icon="lastSubMenu.icon.substr(3)" />
                            <FontAwesome v-else-if="lastSubMenu.icon.startsWith('fas-')" class="w-6 h-6 m-auto" type="fas" :icon="lastSubMenu.icon.substr(4)" />
                            <FontAwesome v-else-if="lastSubMenu.icon.startsWith('far-')" class="w-6 h-6 m-auto" type="far" :icon="lastSubMenu.icon.substr(4)" />
                            <img v-else-if="lastSubMenu.icon.includes('/')" :src="lastSubMenu.icon" class="m-auto" />
                            <component v-else-if="lastSubMenu.icon" :is="lastSubMenu.icon" class="m-auto" />
                            <ZapIcon v-else class="m-auto" />
                          </div>
                          <div class="menu__title">
                            {{ $t(lastSubMenu.title) }}
                          </div>
                        </a>
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
    </transition>
  </div>
  <!-- END: Mobile Menu -->
</template>

<script setup>
import context, { computed, onMounted, ref, watch } from "@cloudfun/core";
import { helper as $h } from "@/utils/helper";
import {
  activeMobileMenu,
  toggleMobileMenu,
  linkTo,
  enter,
  leave,
} from "./index";

const formattedMenu = ref([]);
const mobileMenu = computed(() => context.current.menu);

watch(
  computed(() => context.route.fullPath),
  () => {
    formattedMenu.value = $h.toRaw(mobileMenu.value);
  }
);

onMounted(() => {
  formattedMenu.value = $h.toRaw(mobileMenu.value);
});
</script>
