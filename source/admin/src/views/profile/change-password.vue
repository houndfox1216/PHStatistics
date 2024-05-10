<template>
  <div class="intro-y flex items-center mt-8 sm:hidden">
    <h2 class="text-lg font-medium mr-auto">{{$breadcrumb[$breadcrumb.length-1].title}}</h2>
  </div>
  <div class="intro-y box mt-8 sm:mt-5">
    <div class="p-5">
      <div>
        <div class="flex">
          <label for="change-password-form-1" class="form-label">{{ $t("app.profile.change-password.old-password") }}</label>
          <template v-if="validate.oldPassword.$error">
            <div
              v-for="(error, index) in validate.oldPassword.$errors"
              :key="index"
              class="text-danger ml-auto"
            >
              {{ error.$message }}
            </div>
          </template>
        </div>
        <input
          id="change-password-form-1"
          type="password"
          class="form-control"
          v-model.trim="validate.oldPassword.$model"
        />
      </div>
      <div class="mt-3">
        <div class="flex">
          <label for="change-password-form-2" class="form-label">{{ $t("app.profile.change-password.new-password") }}</label>
          <template v-if="validate.newPassword.$error">
            <div
              v-for="(error, index) in validate.newPassword.$errors"
              :key="index"
              class="text-danger ml-auto"
            >
              {{ error.$message }}
            </div>
          </template>
        </div>
        <input
          id="change-password-form-2"
          type="password"
          class="form-control"
          v-model.trim="validate.newPassword.$model"
        />
      </div>
      <div class="mt-3">
        <div class="flex">
          <label for="change-password-form-3" class="form-label">{{ $t("app.profile.change-password.confirm-password") }}</label>
          <template v-if="validate.confirmPassword.$error">
            <div
              v-for="(error, index) in validate.confirmPassword.$errors"
              :key="index"
              class="text-danger ml-auto"
            >
              {{ error.$message }}
            </div>
          </template>
        </div>
        <input
          id="change-password-form-3"
          type="password"
          class="form-control"
          v-model.trim="validate.confirmPassword.$model"
        />
      </div>
      <button type="button" class="btn btn-primary mt-4" @click="change">
        {{ $t("button.change-password") }}
      </button>
    </div>
  </div>
</template>

<script setup>
import context, { reactive } from "@cloudfun/core";
import { useVuelidate } from "@vuelidate/core";
import { required, helpers } from "@vuelidate/validators";

const state = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: '',
});

const rules = {
  oldPassword: { required },
  newPassword: { format: helpers.withMessage("須8碼以上含大小寫英文、數字", value => new RegExp("^((?=.{8,}$)(?=.*\\d)(?=.*[a-z])(?=.*[A-Z]).*|(?=.{8,}$)(?=.*\\d)(?=.*[a-zA-Z])(?=.*[!\\u0022#$%&'()*+,./:;<=>?@[\\]\\^_`{|}~-]).*)").test(value)) },
  confirmPassword: { samAsNewPassword: helpers.withMessage("Must be equal to new password", value => state.newPassword === value) },
};

const validate = useVuelidate(rules, state);

const change = () => {
  validate.value.$touch();
  if (!validate.value.$invalid) {
    context.root.model.dispatch('user/changePassword', state).then(
      () => context.send('info', { subject: '更新成功', content: '密碼已變更' }),
      reason => context.send('error', { subject: '更新失敗', content: reason })
    )
  }
}
</script>