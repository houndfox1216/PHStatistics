<template>
  <!-- BEGIN: Top Bar -->
  <div class="top-bar">
    <router-link v-if="!activeMenu" to="/" tag="a" class="intro-x hidden sm:flex items-center bg-primary dark:bg-darkmode-800 rounded-md rounded-tl-3xl rounded-br-3xl p-3 -ml-3 mr-3" :title="$model.state.configuration.value.AdminTitle || 'CloudFun Admin'">
      <img class="w-6" src="@/assets/images/logo.svg" />
      <span class="hidden xl:block text-white text-lg ml-1"> {{$model.state.configuration.value.AdminTitle || 'CloudFun Admin'}} </span>
    </router-link>
    <FontAwesome 
      class="hidden sm:flex rounded-md border-2 w-8 h-8 mr-3 p-0.5 border-primary/50 text-primary/80"
      :class="{
        'opacity-30': !activeMenu,
      }"
      icon="bars" 
      @click="toogleMenu"
    />
    <!-- BEGIN: Breadcrumb -->
    <nav aria-label="breadcrumb" class="-intro-x mr-auto hidden sm:flex">
      <ol class="breadcrumb">
        <li 
          v-for="(node, index) in $breadcrumb" 
          :key="`breadcrumb-${index}`"
          class="breadcrumb-item"
          :class="{ active: $breadcrumb.length === index + 1 }"
          :aria-current="node.to && $breadcrumb.length > index + 1 ? 'page' : undefined"
        >
          <span v-if="$breadcrumb.length === index +1" class="text-lg font-medium">{{ $t(node.title) }}</span>
          <a v-else-if="node.to" :href="node.to">{{ $t(node.title) }}</a>
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
      <a class="notification sm:hidden" href="">
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
    <Dropdown class="intro-x mr-auto sm:mr-6">
      <DropdownToggle
        tag="div"
        role="button"
        class="notification notification--bullet cursor-pointer"
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
                class="w-3 h-3 bg-success absolute right-0 bottom-0 rounded-full border-2 border-white dark:border-darkmode-600"
              ></div>
            </div>
            <div class="ml-2 overflow-hidden">
              <div class="flex items-center">
                <a href="javascript:;" class="font-medium truncate mr-5">{{
                  faker.users[0].name
                }}</a>
                <div class="text-xs text-slate-400 ml-auto whitespace-nowrap">
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

    <Dropdown class="mr-3">
      <DropdownToggle class="btn btn-primary">
        {{ $t("locale.short-name") }}
      </DropdownToggle>
      <DropdownMenu>
        <DropdownContent>
          <DropdownItem v-for="item in $locales" @click="() => changeLocale(item)">
            {{ $t("locale.short-name", item) }}
          </DropdownItem>
        </DropdownContent>
      </DropdownMenu>
    </Dropdown>

    <!-- BEGIN: Account Menu -->
    <Dropdown class="intro-x w-8 h-8">
      <DropdownToggle
        tag="div"
        role="button"
        class="w-8 h-8 rounded-full overflow-hidden shadow-lg image-fit zoom-in"
      >
        <img
          :alt="$user.Name"
          :src="$user.PhotoUri"
          :title="$user.Name"
          onerror="this.onerror=null;this.src='/images/midone.svg'"
        />
      </DropdownToggle>
      <DropdownMenu class="w-56" :hidden="accountMenuHidden">
        <DropdownContent class="bg-primary text-white">
          <DropdownHeader tag="div" class="!font-normal">
            <div class="text-xs text-white/70 mt-0.5 dark:text-slate-500">
              {{ $t("layout.signed-in-as") }}:&nbsp;
              <span v-if="$user.Id">({{$user.Account}})</span>
            </div>
            <div class="font-medium pt-2 pl-2">
              {{ $user.Name }}
            </div>
          </DropdownHeader>
          <DropdownDivider v-if="$user.Id" class="border-white/[0.08]"/>
          <DropdownItem v-if="$user.Id" class="hover:bg-white/5" @click="() => $router.push(`${prefix}/profile`)">
            <UserIcon class="w-4 h-4 mr-2" /> {{ $t("layout.edit-profile") }}
          </DropdownItem>
          <DropdownItem v-if="$user.Id" class="hover:bg-white/5" @click="() => $router.push(`${prefix}/profile/change-password`)">
            <LockIcon class="w-4 h-4 mr-2" /> {{ $t("layout.change-password") }}
          </DropdownItem>
          <DropdownItem v-if="$user.Id" class="hover:bg-white/5">
            <a href="https://www.cloudfun.com.tw/#about-us" target="_blank0" class="flex w-full"><HelpCircleIcon class="w-4 h-4 mr-2" /> Help</a>
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
  <!-- END: Top Bar -->
</template>

<script setup>
import { onBeforeMount } from "vue";
import context, { ref, computed, nextTick } from "@cloudfun/core";
import { locale } from 'devextreme/localization';

const accountMenuHidden = ref(false);
const prefix = context.route.path.startsWith("/simple-menu") ? "/simple-menu" : "";

const searchDropdown = ref(false);
const showSearchDropdown = () => {
  searchDropdown.value = true;
};
const hideSearchDropdown = () => {
  searchDropdown.value = false;
};

const logout = () => {
  accountMenuHidden.value = true;
  const router = context.router;
  const model = context.model;
  if (context.user.Id) {
    model.dispatch('logout').finally(() => {
      model.user = model.guest;
      router.push('/login');
    });
  } else router.push('/login');
};

onBeforeMount(() => {
  const currentLocale = localStorage.getItem("locale");
  context.root.locale = currentLocale;
  locale(currentLocale);
})

const changeLocale = (item) => {
  console.log('current locale: ', context.root.locale)
  if (item === context.root.locale) return;
  localStorage.setItem("locale", item);
  context.root.locale = item;
  locale(item);
  document.location.reload();
}

const model = context.current.model;

const activeMenu = computed(() => model.getters["midone/activeMenu"]);
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

function toogleMenu() {
  model.commit("midone/setActiveMenu", !model.getters["midone/activeMenu"]);
}
</script>
