<template>
  <div>
    <div class="container sm:px-10">
      <div class="block xl:grid grid-cols-2 gap-4">
        <!-- BEGIN: Login Info -->
        <div class="hidden xl:flex flex-col min-h-screen">
          <a href="" class="-intro-x flex items-center pt-5">
            <img
              alt="Midone Tailwind HTML Admin Template"
              class="w-6"
              src="@/assets/images/logo.svg"
            />
            <span class="text-white text-lg ml-3"> {{$model.state.configuration.value.AdminTitle || 'CloudFun Admin'}} </span>
          </a>
          <div class="my-auto">
            <img
              alt="Midone Tailwind HTML Admin Template"
              class="-intro-x w-1/2 -mt-16"
              src="@/assets/images/illustration.svg"
            />
            <div class="-intro-x w-2/3 break-words text-white font-medium text-4xl leading-tight mt-10" v-html="$t('app.login.description')"></div>
            <div class="-intro-x w-2/3 break-words mt-5 text-lg text-white text-opacity-70 dark:text-slate-400" v-html="$t('app.login.introduction')"></div>
          </div>
        </div>
        <!-- END: Login Info -->
        <!-- BEGIN: Login Form -->
        <div class="h-screen xl:h-auto flex py-5 xl:py-0 my-10 xl:my-0">
          <div
            class="my-auto mx-auto xl:ml-20 bg-white dark:bg-darkmode-600 xl:bg-transparent px-5 sm:px-8 py-8 xl:p-0 rounded-md shadow-md xl:shadow-none w-full sm:w-3/4 lg:w-2/4 xl:w-auto"
          >
            <h2
              class="intro-x font-bold text-2xl xl:text-3xl text-center xl:text-left"
            >
              {{ $t('app.login.title') }}
            </h2>
            <div class="intro-x mt-2 text-slate-600 xl:hidden text-center" v-html="$t('app.login.description')"></div>
            <div class="intro-x text-slate-400 xl:hidden text-center" v-html="$t('app.login.introduction')"></div>
            <form @submit.prevent="formSubmit()">
              <div class="intro-x mt-8">
                <input
                  type="text"
                  class="intro-x login__input form-control py-3 px-4 block"
                  :placeholder="$t('app.login.placeholder.account')"
                  v-model="data.account"
                />
                <div class="input-group mt-4">
                  <input
                    ref="passwordInput"
                    type="password"
                    class="intro-x form-control py-3 px-4"
                    :placeholder="$t('app.login.placeholder.password')"
                    v-model="data.password"
                  />
                  <div 
                    class="input-group-text !px-1 flex items-center cursor-pointer"
                    title="show/hide password"
                    @click="togglePasswordIcon">
                    <component :is="passwordIcon" class="text-slate-400" />
                  </div>
                </div>
                <div class="input-group mt-4">
                  <input 
                    type="text" 
                    class="intro-x form-control py-3 px-4" 
                    :placeholder="$t('app.login.placeholder.captcha')"
                    aria-label="Captcha code" 
                    aria-describedby="input-group-captcha"
                    v-model="data.captcha"
                  />
                  <div id="input-group-captcha" class="input-group-text !p-0">
                    <img 
                      class="h-full border-0 rounded-r-lg cursor-pointer"
                      title="click me to refresh"
                      :src="captchaUrl"
                      @click="data.captchaToken = uuid()"
                    />
                  </div>
                </div>                
              </div>
              <div class="intro-x flex text-slate-600 dark:text-slate-500 text-xs sm:text-sm mt-4">
                <div class="flex items-center mr-auto">
                  <input
                    id="remember-me"
                    type="checkbox"
                    class="form-check-input border mr-2"
                    v-model="data.rememberMe"
                  />
                  <label class="cursor-pointer select-none" for="remember-me">{{ $t('app.login.remember-me') }}</label
                  >
                </div>
              </div>
              <div class="intro-x mt-5 xl:mt-8 text-center xl:text-left">
                <button
                  class="btn btn-primary py-3 px-4 w-full xl:w-32 xl:mr-3 align-top"
                >
                  {{ $t('button.login') }}
                </button>
              </div>
            </form>
          </div>
        </div>
        <!-- END: Login Form -->
      </div>
    </div>
  </div>
</template>

<script setup>
import context, { ref, reactive, computed, onMounted } from "@cloudfun/core";
import dom from "@left4code/tw-starter/dist/js/dom";
import { v1 as uuid } from 'uuid'

const application = context.current;
const model = application.model;

const data = reactive({ account: model.getters["midone/lastLogin"], password: "", captchaToken: uuid(), rememberMe: false});
const captchaUrl = computed(() => `${import.meta.env.VITE_SERVICE_URI}/api/captcha?token=${data.captchaToken}`)
const passwordInput = ref();
const passwordIcon = ref("EyeOffIcon");

const formSubmit = () => {
  model.login(data).then(
    () => {
      model.commit("midone/setLastLogin", data.account);
      application.policy.router.push('/');
    },
    failure => {
      application.user = context.guest;
      data.captchaToken = uuid();
      context.send('warning', failure.message || '帳號或密碼錯誤')
    }
  );
};

const togglePasswordIcon = () => {
  passwordInput.value.type = passwordIcon.value == "EyeIcon" ? "password" : "text";
  passwordIcon.value = passwordIcon.value == "EyeIcon" ? "EyeOffIcon" : "EyeIcon";
}

onMounted(() => {
  dom("body").removeClass("main").removeClass("error-page").addClass("login");
});
</script>
