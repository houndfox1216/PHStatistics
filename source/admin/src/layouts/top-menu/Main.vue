<template>
  <div class="py-2">
    <MobileMenu />
    <!-- BEGIN: Top Bar -->
    <div
      class="border-b border-white/[0.08] -mt-10 md:-mt-5 -mx-3 sm:-mx-8 px-3 sm:px-8 pt-3 md:pt-0 mb-3"
    >
      <div class="top-bar-boxed flex items-center">
        <!-- BEGIN: Logo -->
        <router-link to="/" tag="a" class="-intro-x hidden md:flex" :title="$model.state.configuration.value.AdminTitle || 'CloudFun Admin'">
          <img class="w-6" src="@/assets/images/logo.svg" />
          <span class="text-white text-lg ml-3">{{$model.state.configuration.value.AdminTitle || 'CloudFun Admin'}}</span>
        </router-link>
        <!-- END: Logo -->
        <!-- BEGIN: Breadcrumb -->
        <nav aria-label="breadcrumb" class="-intro-x h-full mr-auto">
          <ol class="breadcrumb breadcrumb-light">
            <li 
              v-for="(node, index) in $breadcrumb" 
              :key="`breadcrumb-${index}`"
              class="breadcrumb-item"
              :class="{ active: $breadcrumb.length === index + 1 }"
              :aria-current="node.to && $breadcrumb.length > index + 1 ? 'page' : undefined"
            >
              <a v-if="node.to && $breadcrumb.length > index + 1" :href="node.to">{{ $t(node.title) }}</a>
              <span v-else>{{ $t(node.title) }}</span>
            </li>
          </ol>
        </nav>
        <!-- END: Breadcrumb -->
        <!-- BEGIN: Search -->
        <div class="intro-x relative mr-3 sm:mr-6">
          <div class="search hidden sm:block">
            <input
              type="text"
              class="search__input form-control border-transparent"
              placeholder="Search..."
              @focus="showSearchDropdown"
              @blur="hideSearchDropdown"
            />
            <SearchIcon class="search__icon dark:text-slate-500" />
          </div>
          <a class="notification notification--light sm:hidden" href="">
            <SearchIcon class="notification__icon dark:text-slate-500" />
          </a>
          <div class="search-result" :class="{ show: searchDropdown }">
            <div class="search-result__content">
              <div class="search-result__content__title">Pages</div>
              <div class="mb-5">
                <a href="" class="flex items-center">
                  <div
                    class="w-8 h-8 bg-success/20 dark:bg-success/10 text-success flex items-center justify-center rounded-full"
                  >
                    <InboxIcon class="w-4 h-4" />
                  </div>
                  <div class="ml-3">Mail Settings</div>
                </a>
                <a href="" class="flex items-center mt-2">
                  <div
                    class="w-8 h-8 bg-pending/10 text-pending flex items-center justify-center rounded-full"
                  >
                    <UsersIcon class="w-4 h-4" />
                  </div>
                  <div class="ml-3">Users & Permissions</div>
                </a>
                <a href="" class="flex items-center mt-2">
                  <div
                    class="w-8 h-8 bg-primary/10 dark:bg-primary/20 text-primary/80 flex items-center justify-center rounded-full"
                  >
                    <CreditCardIcon class="w-4 h-4" />
                  </div>
                  <div class="ml-3">Transactions Report</div>
                </a>
              </div>
              <div class="search-result__content__title">Users</div>
              <div class="mb-5">
                <a
                  v-for="(faker, fakerKey) in $_.take($f(), 4)"
                  :key="fakerKey"
                  href
                  class="flex items-center mt-2"
                >
                  <div class="w-8 h-8 image-fit">
                    <img
                      alt="Midone Tailwind HTML Admin Template"
                      class="rounded-full"
                      :src="faker.photos[0]"
                    />
                  </div>
                  <div class="ml-3">{{ faker.users[0].name }}</div>
                  <div
                    class="ml-auto w-48 truncate text-slate-500 text-xs text-right"
                  >
                    {{ faker.users[0].email }}
                  </div>
                </a>
              </div>
              <div class="search-result__content__title">Products</div>
              <a
                v-for="(faker, fakerKey) in $_.take($f(), 4)"
                :key="fakerKey"
                href
                class="flex items-center mt-2"
              >
                <div class="w-8 h-8 image-fit">
                  <img
                    alt="Midone Tailwind HTML Admin Template"
                    class="rounded-full"
                    :src="faker.images[0]"
                  />
                </div>
                <div class="ml-3">{{ faker.products[0].name }}</div>
                <div
                  class="ml-auto w-48 truncate text-slate-500 text-xs text-right"
                >
                  {{ faker.products[0].category }}
                </div>
              </a>
            </div>
          </div>
        </div>
        <!-- END: Search -->
        <!-- BEGIN: Notifications -->
        <Dropdown class="intro-x mr-4 sm:mr-6">
          <DropdownToggle
            tag="div"
            role="button"
            class="notification notification--light notification--bullet cursor-pointer"
          >
            <BellIcon class="notification__icon dark:text-slate-500" />
          </DropdownToggle>
          <DropdownMenu class="notification-content pt-2">
            <DropdownContent tag="div" class="notification-content__box">
              <div class="notification-content__title">Notifications</div>
              <div
                v-for="(faker, fakerKey) in $_.take($f(), 5)"
                :key="fakerKey"
                class="cursor-pointer relative flex items-center"
                :class="{ 'mt-5': fakerKey }"
              >
                <div class="w-12 h-12 flex-none image-fit mr-1">
                  <img
                    alt="Midone Tailwind HTML Admin Template"
                    class="rounded-full"
                    :src="faker.photos[0]"
                  />
                  <div
                    class="w-3 h-3 bg-success absolute right-0 bottom-0 rounded-full border-2 border-white"
                  ></div>
                </div>
                <div class="ml-2 overflow-hidden">
                  <div class="flex items-center">
                    <a href="javascript:;" class="font-medium truncate mr-5">{{
                      faker.users[0].name
                    }}</a>
                    <div
                      class="text-xs text-slate-400 ml-auto whitespace-nowrap"
                    >
                      {{ faker.times[0] }}
                    </div>
                  </div>
                  <div class="w-full truncate text-slate-500 mt-0.5">
                    {{ faker.news[0].shortContent }}
                  </div>
                </div>
              </div>
            </DropdownContent>
          </DropdownMenu>
        </Dropdown>
        <!-- END: Notifications -->
        <!-- BEGIN: Account Menu -->
        <Dropdown class="intro-x w-8 h-8">
          <DropdownToggle
            tag="div"
            role="button"
            class="w-8 h-8 rounded-full overflow-hidden shadow-lg image-fit zoom-in scale-110"
          >
            <img
              :alt="$user.Name"
              :src="$user.PhotoUri"
              :title="$user.Name"
              onerror="this.onerror=null;this.src='/images/midone.svg'"
            />
          </DropdownToggle>
          <DropdownMenu class="w-56" :hidden="accountMenuHidden">
            <DropdownContent class="bg-primary/80 before:block before:absolute before:bg-black before:inset-0 before:rounded-md before:z-[-1] text-white">
              <DropdownHeader tag="div" class="!font-normal">
                <div class="text-xs text-white/70 mt-0.5 dark:text-slate-500">
                  {{ $t("layout.signed-in-as") }}:&nbsp;
                  <span v-if="$user.Id">({{$user.Account}})</span>
                </div>
                <div class="font-medium">
                  {{ $user.Name }}
                </div>
              </DropdownHeader>
              <DropdownDivider v-if="$user.Id" class="border-white/[0.08]" />
              <DropdownItem v-if="$user.Id" class="hover:bg-white/5" @click="() => $router.push('/top-menu/profile')">
                <UserIcon class="w-4 h-4 mr-2" /> {{ $t("layout.edit-profile") }}
              </DropdownItem>
              <DropdownItem v-if="$user.Id" class="hover:bg-white/5" @click="() => $router.push('/top-menu/profile/change-password')">
                <LockIcon class="w-4 h-4 mr-2" /> {{ $t("layout.change-password") }}
              </DropdownItem>
              <DropdownItem v-if="$user.Id" class="hover:bg-white/5">
                <a href="https://www.cloudfun.com.tw/#about-us" target="_blank0" class="flex w-full"><HelpCircleIcon class="w-4 h-4 mr-2" /> {{ $t("layout.help") }}</a>
              </DropdownItem>
              <DropdownDivider class="border-white/[0.08]" />
              <DropdownItem class="hover:bg-white/5" @click="logout">
                <ToggleLeftIcon class="w-4 h-4 mr-2" /> {{ $t($user.Id ? 'layout.logout' : 'layout.login') }}
              </DropdownItem>
              <DropdownDivider class="border-white/[0.08]" />
              <DropdownHeader tag="div" class="!font-normal !py-0">
                <div class="text-xs text-white/70 my-0.5 dark:text-slate-500">
                  {{ $t('layout.color-scheme') }}:
                </div>
                <div class="flex justify-center rounded-md shadow-sm" role="group">
                  <a
                    @click="switchColorScheme('default')"
                    class="block w-8 h-8 cursor-pointer bg-sky-700 rounded-full border-2 mr-1 hover:border-slate-200"
                    :class="{
                      '!border-warning': colorScheme == 'default',
                      'border-white/70 dark:!border-slate-500': colorScheme != 'default',
                    }"
                  ></a>
                  <a
                    @click="switchColorScheme('theme-1')"
                    class="block w-8 h-8 cursor-pointer bg-emerald-800 rounded-full border-2 mr-1 hover:border-slate-200"
                    :class="{
                      '!border-warning': colorScheme == 'theme-1',
                      'border-white/70 dark:!border-slate-500': colorScheme != 'theme-1',
                    }"
                  ></a>
                  <a
                    @click="switchColorScheme('theme-2')"
                    class="block w-8 h-8 cursor-pointer bg-pink-800 rounded-full border-2 mr-1 hover:border-slate-200"
                    :class="{
                      '!border-warning': colorScheme == 'theme-2',
                      'border-white/70 dark:!border-slate-500': colorScheme != 'theme-2',
                    }"
                  ></a>
                  <a
                    @click="switchColorScheme('theme-3')"
                    class="block w-8 h-8 cursor-pointer bg-amber-700 rounded-full border-2 mr-1 hover:border-slate-200"
                    :class="{
                      '!border-warning': colorScheme == 'theme-3',
                      'border-white/70 dark:!border-slate-500': colorScheme != 'theme-3',
                    }"
                  ></a>
                  <a
                    @click="switchColorScheme('theme-4')"
                    class="block w-8 h-8 cursor-pointer bg-slate-700 rounded-full border-2 hover:border-slate-200"
                    :class="{
                      '!border-warning': colorScheme == 'theme-4',
                      'border-white/70 dark:!border-slate-500': colorScheme != 'theme-4',
                    }"
                  ></a>
                </div>
              </DropdownHeader>
              <DropdownDivider class="border-white/[0.08]" />
              <DropdownHeader tag="div" class="!font-normal !py-0">
                <div class="text-xs text-white/70 my-0.5 dark:text-slate-500">
                  {{ $t('layout.dark-mode.title') }}:
                </div>
                <div class="flex justify-center rounded-md shadow-sm mb-2" role="group">
                  <button 
                    type="button" 
                    :class="`px-3 py-1 text-sm font-medium ${darkModeValue === false ? 'text-warning' : 'text-slate-300'} bg-white/20 border rounded-l-lg hover:bg-white/70 hover:text-primary`"
                    @click="switchDarkMode(false)"
                  >
                    {{ $t('layout.dark-mode.light') }}
                  </button>
                  <button 
                    type="button" 
                    :class="`px-5 py-1 text-sm font-medium ${darkModeValue === true ? 'text-warning' : 'text-slate-300'} bg-white/20 border-t border-b hover:bg-white/70 hover:text-primary`"
                    @click="switchDarkMode(true)"
                  >
                    {{ $t('layout.dark-mode.dark') }}
                  </button>
                  <button 
                    type="button" 
                    :class="`px-2 py-1 text-sm font-medium ${darkModeValue === undefined ? 'text-warning' : 'text-slate-300'} bg-white/20 border rounded-r-lg hover:bg-white/70 hover:text-primary`"
                    @click="switchDarkMode(undefined)"
                  >
                    {{ $t('layout.dark-mode.system') }}
                  </button>
                </div>
              </DropdownHeader>
              <DropdownDivider class="border-white/[0.08]" />
              <DropdownHeader tag="div" class="!font-normal !py-0">
                <div class="text-xs text-white/70 my-0.5 dark:text-slate-500">
                  {{ $t('layout.menu-mode.title') }}:
                </div>
                <div class="flex justify-center rounded-md shadow-sm mb-2" role="group">
                  <button 
                    type="button" 
                    :class="`px-4 py-1 text-sm font-medium ${layoutModeValue === 'side' ? 'text-warning' : 'text-slate-300'} bg-white/20 border rounded-l-lg hover:bg-white/70 hover:text-primary`"
                    @click="switchLayoutMode('side')"
                  >
                    {{ $t('layout.menu-mode.side') }}
                  </button>
                  <button 
                    type="button" 
                    :class="`px-3 py-1 text-sm font-medium ${layoutModeValue === 'simple' ? 'text-warning' : 'text-slate-300'} bg-white/20 border-t border-b hover:bg-white/70 hover:text-primary`"
                    @click="switchLayoutMode('simple')"
                  >
                    {{ $t('layout.menu-mode.simple') }}
                  </button>
                  <button 
                    type="button" 
                    :class="`px-5 py-1 text-sm font-medium ${layoutModeValue === 'top' ? 'text-warning' : 'text-slate-300'} bg-white/20 border rounded-r-lg hover:bg-white/70 hover:text-primary`"
                    @click="switchLayoutMode('top')"
                  >
                    {{ $t('layout.menu-mode.top') }}
                  </button>
                </div>
              </DropdownHeader>
            </DropdownContent>
          </DropdownMenu>
        </Dropdown>
        <!-- END: Account Menu -->
      </div>
    </div>
    <!-- END: Top Bar -->
    <!-- BEGIN: Top Menu -->
    <nav class="top-nav">
      <ul>
        <li v-for="(menu, menuKey) in formattedMenu" 
          :key="menuKey" 
          :class="typeof menu === 'string' && menu !== 'devider' ? 'basis-full w-0;' : undefined"
        >
          <div v-if="typeof menu === 'string' && menu !== 'devider'" class="flex my-2 border-b border-white/60">
            <div class="rounded-t-lg border-x border-t border-white/60 text-xs text-white/60 px-2">{{ menu }}</div>
          </div>
          <div v-else-if="typeof menu === 'string'" class="border-l mr-1 h-full border-white/60"></div>
          <a v-else 
            :href="menu.subNodes || !menu.to ? 'javascript:;' : menu.to" class="top-menu"
            :class="{ 'top-menu--active': menu.active }"
            @click="linkTo(menu, $router, $event)"
          >
            <div class="top-menu__icon">
              <ActivityIcon v-if="!menu.icon" />
              <img v-else-if="menu.icon.includes('/')" :src="menu.icon" />
              <FontAwesome v-else-if="menu.icon.startsWith('fa-')" class="w-6 h-6" :icon="menu.icon.substr(3)" />
              <FontAwesome v-else-if="menu.icon.startsWith('fas-')" class="w-6 h-6" type="fas" :icon="menu.icon.substr(4)" />
              <FontAwesome v-else-if="menu.icon.startsWith('far-')" class="w-6 h-6" type="far" :icon="menu.icon.substr(4)" />
              <component v-else :is="menu.icon" />
            </div>
            <div class="top-menu__title">
              {{ $t(menu.title) }}
              <ChevronDownIcon v-if="menu.subNodes" class="top-menu__sub-icon" />
            </div>
          </a>
          <!-- BEGIN: Second Child -->
          <ul v-if="menu.subNodes">
            <li v-for="(subMenu, subMenuKey) in menu.subNodes.filter(e => typeof e !== 'string')" :key="subMenuKey">
              <a
                :href="subMenu.subNodes || !subMenu.to ? 'javascript:;' : subMenu.to"
                class="top-menu"
                @click="linkTo(subMenu, $router, $event)"
              >
                <div class="top-menu__icon">
                  <ActivityIcon v-if="!subMenu.icon" />
                  <img v-else-if="subMenu.icon.includes('/')" :src="subMenu.icon" />
                  <FontAwesome v-else-if="subMenu.icon.startsWith('fa-')" class="w-6 h-6" :icon="subMenu.icon.substr(3)" />
                  <FontAwesome v-else-if="subMenu.icon.startsWith('fas-')" class="w-6 h-6" type="fas" :icon="subMenu.icon.substr(4)" />
                  <FontAwesome v-else-if="subMenu.icon.startsWith('far-')" class="w-6 h-6" type="far" :icon="subMenu.icon.substr(4)" />
                  <component v-else :is="subMenu.icon" />
                </div>
                <div class="top-menu__title">
                  {{ $t(subMenu.title) }}
                  <ChevronDownIcon v-if="subMenu.subNodes" class="top-menu__sub-icon" />
                </div>
              </a>
              <!-- BEGIN: Third Child -->
              <ul v-if="subMenu.subNodes">
                <li v-for="(lastSubMenu, lastSubMenuKey) in subMenu.subNodes.filter(e => typeof e !== 'string')" :key="lastSubMenuKey">
                  <a
                    :href="lastSubMenu.subNodes || !lastSubMenu.to ? 'javascript:;' : lastSubMenu.to"
                    class="top-menu"
                    @click="linkTo(lastSubMenu, $router, $event)"
                  >
                    <div class="top-menu__icon">
                      <ZapIcon v-if="!lastSubMenu.icon" />
                      <FontAwesome v-else-if="lastSubMenu.icon.startsWith('fa-')" class="w-6 h-6" :icon="lastSubMenu.icon.substr(3)" />
                      <FontAwesome v-else-if="lastSubMenu.icon.startsWith('fas-')" class="w-6 h-6" type="fas" :icon="lastSubMenu.icon.substr(4)" />
                      <FontAwesome v-else-if="lastSubMenu.icon.startsWith('far-')" class="w-6 h-6" type="far" :icon="lastSubMenu.icon.substr(4)" />
                      <img v-else-if="lastSubMenu.icon.includes('/')" :src="lastSubMenu.icon" />
                      <component v-else :is="lastSubMenu.icon" />
                    </div>
                    <div class="top-menu__title">
                      {{ $t(lastSubMenu.title) }}
                    </div>
                  </a>
                </li>
              </ul>
              <!-- END: Third Child -->
            </li>
          </ul>
          <!-- END: Second Child -->
        </li>
      </ul>
    </nav>
    <!-- END: Top Menu -->
    <!-- BEGIN: Content -->
    <div class="content">
      <router-view />
    </div>
    <!-- END: Content -->
  </div>
</template>

<script setup>
import context, { computed, onMounted, ref, watch, nextTick } from "@cloudfun/core";
import { helper as $h } from "@/utils/helper";
import MobileMenu from "@/components/mobile-menu/Main.vue";
import {
  searchDropdown,
  showSearchDropdown,
  hideSearchDropdown,
} from "./index";
import { linkTo } from "@/layouts/side-menu";
import dom from "@left4code/tw-starter/dist/js/dom";

const application = context.current;
const accountMenuHidden = ref(false);
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

const logout = () => {
  accountMenuHidden.value = true;
  if (context.user.Id) {
    application.model.dispatch('logout').finally(() => {
      application.model.user = context.guest;
      context.router.push('/login');
    });
  } else context.router.push('/login');
};

const model = context.current.model;

const colorScheme = computed(() => model.getters["midone/colorScheme"]);
const darkMode = computed(() => model.getters["midone/darkMode"]);
const darkModeValue = computed(() => model.state.midone.darkModeValue);

const setColorSchemeClass = () => {
  const elements = document.getElementsByTagName("html");
  if (!elements.length) return; // html tag isn't exist
  elements[0].classList.remove("default");
  elements[0].classList.remove("theme-1");
  elements[0].classList.remove("theme-2");
  elements[0].classList.remove("theme-3");
  elements[0].classList.remove("theme-4");
  elements[0].classList.add(colorScheme.value);
};

const switchColorScheme = (colorScheme) => {
  model.commit("midone/setColorScheme", colorScheme);
  setColorSchemeClass();
};

setColorSchemeClass();

const setDarkModeClass = () => {
  const elements = document.getElementsByTagName("html");
  if (!elements.length) return; // html tag isn't exist
  darkMode.value 
    ? elements[0].classList.add("dark")
    : elements[0].classList.remove("dark");
};

function switchDarkMode(value) {
  model.commit("midone/setDarkMode", value);
  setDarkModeClass();
}

setDarkModeClass();

const layoutModeValue = computed(() => model.state.midone.layoutModeValue);

function switchLayoutMode(value) {
  accountMenuHidden.value = true;
  nextTick(() => {
    if (value === layoutModeValue) return;
    model.commit("midone/setLayoutMode", value);
    accountMenuHidden.value = false;
  })
}
</script>
