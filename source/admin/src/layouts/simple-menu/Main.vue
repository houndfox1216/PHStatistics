<template>
  <div>
    <MobileMenu />
    <div class="flex">
      <!-- BEGIN: Simple Menu -->
      <nav class="side-nav side-nav--simple" v-if="$model.getters['midone/activeMenu']">
        <router-link to="/" tag="a" class="intro-x flex items-center pl-1 pt-4" :title="$model.state.configuration.value.AdminTitle || 'CloudFun Admin'">
          <img
            alt="Midone Tailwind HTML Admin Template"
            class="side-nav__logo"
            src="@/assets/images/logo.svg"
          />
        </router-link>
        <div class="side-nav__devider my-6"></div>
        <ul>
          <!-- BEGIN: First Child -->
          <template v-for="(menu, menuKey) in formattedMenu">
            <li v-if="typeof menu === 'string'"
              :key="`devider-${menuKey}`"
              class="side-nav__devider my-6 flex items-center"
            >
              <div v-if="menu !== 'devider'" class="w-full rounded-lg bg-white/40 text-xs text-slate-200 text-center">{{ menu }}</div>
            </li>
            <li v-else :key="`menu-${menuKey}`">
              <Tippy
                tag="a"
                :content="menu.title"
                :options="{
                  placement: 'left',
                }"
                :href="
                  menu.subNodes || !menu.to
                    ? 'javascript:;'
                    : menu.to
                "
                class="side-menu"
                :class="{
                  'side-menu--active': menu.active,
                  'side-menu--open': menu.activeDropdown,
                  'side-menu--cabinet': menu.subNodes !== undefined,
                }"
                @click="linkTo(menu, $router, $event)"
              >
                <div class="side-menu__icon flex items-center justify-center">
                  <ActivityIcon v-if="!menu.icon" class="w-8 h-8" />
                  <img v-else-if="menu.icon.includes('/')" :src="menu.icon" class="h-8" />
                  <FontAwesome v-else-if="menu.icon.startsWith('fa-')" class="w-8 h-8" :icon="menu.icon.substr(3)" />
                  <FontAwesome v-else-if="menu.icon.startsWith('fas-')" class="w-8 -8" type="fas" :icon="menu.icon.substr(4)" />
                  <FontAwesome v-else-if="menu.icon.startsWith('far-')" class="w-8 h-8" type="far" :icon="menu.icon.substr(4)" />
                  <component v-else :is="menu.icon" class="w-8 h-8" />
                </div>
                <div class="side-menu__title">
                  <span class="side-menu__title-text">{{ $t(menu.title) }}</span>
                  <ChevronDownIcon
                    v-if="$h.isset(menu.subNodes)"
                    class="side-menu__sub-icon"
                    :class="{ 'transform rotate-180': menu.activeDropdown }"
                  />
                </div>
              </Tippy>
              <!-- BEGIN: Second Child -->
              <transition @enter="enter" @leave="leave">
                <ul v-if="$h.isset(menu.subNodes) && menu.activeDropdown">
                  <li
                    v-for="(subMenu, subMenuKey) in menu.subNodes.filter(e => typeof e !== 'string')"
                    :key="subMenuKey"
                  >
                    <Tippy
                      tag="a"
                      :content="subMenu.title"
                      :options="{
                        placement: 'left',
                      }"
                      :href="
                        subMenu.subNodes || !subMenu.to
                          ? 'javascript:;'
                          : subMenu.to
                      "
                      class="side-menu"
                      :class="{ 
                        'side-menu--active': subMenu.active,
                        'side-menu--cabinet': subMenu.subNodes !== undefined,
                      }"
                      @click="linkTo(subMenu, $router, $event)"
                    >
                      <div class="side-menu__icon flex items-center justify-center">
                        <ActivityIcon v-if="!subMenu.icon" class="w-8 h-8" />
                        <img v-else-if="subMenu.icon.includes('/')" :src="subMenu.icon" class="h-8" />
                        <FontAwesome v-else-if="subMenu.icon.startsWith('fa-')" class="w-8 h-8" :icon="subMenu.icon.substr(3)" />
                        <FontAwesome v-else-if="subMenu.icon.startsWith('fas-')" class="w-8 h-8" type="fas" :icon="subMenu.icon.substr(4)" />
                        <FontAwesome v-else-if="subMenu.icon.startsWith('far-')" class="w-8 h-8" type="far" :icon="subMenu.icon.substr(4)" />
                        <component v-else :is="subMenu.icon" class="w-8 h-8" />
                      </div>
                      <div class="side-menu__title">
                        <span class="side-menu__title-text">{{ $t(subMenu.title) }}</span>
                        <ChevronDownIcon
                          v-if="$h.isset(subMenu.subNodes)"
                          class="side-menu__sub-icon"
                          :class="{ 'transform rotate-180': subMenu.activeDropdown }"
                        />
                      </div>
                    </Tippy>
                    <!-- BEGIN: Third Child -->
                    <transition @enter="enter" @leave="leave">
                      <ul v-if="$h.isset(subMenu.subNodes) && subMenu.activeDropdown">
                        <li
                          v-for="(lastSubMenu, lastSubMenuKey) in subMenu.subNodes.filter(e => typeof e !== 'string')"
                          :key="lastSubMenuKey"
                        >
                          <Tippy
                            tag="a"
                            :content="lastSubMenu.title"
                            :options="{ placement: 'left' }"
                            :href="lastSubMenu.subNodes || !lastSubMenu.to ? 'javascript:;' : lastSubMenu.to"
                            class="side-menu"
                            :class="{ 'side-menu--active': lastSubMenu.active }"
                            @click="linkTo(lastSubMenu, $router, $event)"
                          >
                            <div class="side-menu__icon flex items-center justify-center">
                              <ZapIcon v-if="!lastSubMenu.icon" class="w-8 h-8" />
                              <img v-else-if="lastSubMenu.icon.includes('/')" :src="lastSubMenu.icon" class="h-8" />
                              <FontAwesome v-else-if="lastSubMenu.icon.startsWith('fa-')" class="w-8 h-8" :icon="lastSubMenu.icon.substr(3)" />
                              <FontAwesome v-else-if="lastSubMenu.icon.startsWith('fas-')" class="w-8 h-8" type="fas" :icon="lastSubMenu.icon.substr(4)" />
                              <FontAwesome v-else-if="lastSubMenu.icon.startsWith('far-')" class="w-8 h-8" type="far" :icon="lastSubMenu.icon.substr(4)" />
                              <component v-else :is="lastSubMenu.icon" class="w-8 h-8" />
                            </div>
                            <div class="side-menu__title">
                              <span class="side-menu__title-text">{{ $t(lastSubMenu.title) }}</span>
                            </div>
                          </Tippy>
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
      <!-- END: Simple Menu -->
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
import { linkTo, enter, leave } from "@/layouts/side-menu";
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
