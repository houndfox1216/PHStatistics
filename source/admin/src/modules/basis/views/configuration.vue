<template>
  <div class="intro-y flex items-center mt-8 sm:hidden">
    <h2 class="text-lg font-medium mr-auto">{{$breadcrumb[$breadcrumb.length-1].title}}</h2>
  </div>
  <form class="validate-form" @submit.prevent="save">
    <div class="grid grid-cols-12 gap-6 mt-5">
      <div class="col-span-12 lg:col-span-6">
        <div class="intro-y box p-5 h-full">
          <div>
            <div class="flex">
              <label>{{ $t("app.basis.configuration.admin-title") }}</label>
              <template v-if="validate.AdminTitle.$error">
                <div
                  v-for="(error, index) in validate.AdminTitle.$errors"
                  :key="index"
                  class="text-danger ml-auto"
                >
                  {{ error.$message }}
                </div>
              </template>
            </div>
            <div class="input-group">
              <div class="input-group-text"><FontAwesome icon="quote-right" class="w-4" /></div>
              <input
                type="text"
                name="AdminTitle"
                class="form-control"
                :class="{ 'border-danger': validate.AdminTitle.$error }"
                placeholder="Type your text here"
                v-model.trim="validate.AdminTitle.$model"
              />
            </div>
          </div>
          <div class="mt-3">
            <div class="flex">
              <label>{{ $t("app.basis.configuration.portal-title") }}</label>
              <template v-if="validate.PortalTitle.$error">
                <div
                  v-for="(error, index) in validate.PortalTitle.$errors"
                  :key="index"
                  class="text-danger ml-auto"
                >
                  {{ error.$message }}
                </div>
              </template>
            </div>
            <div class="input-group">
              <div class="input-group-text"><FontAwesome icon="quote-right" class="w-4" /></div>
              <input
                type="text"
                name="PortalTitle"
                class="form-control"
                :class="{ 'border-danger': validate.PortalTitle.$error }"
                placeholder="Type your text here"
                v-model.trim="validate.PortalTitle.$model"
              />
            </div>
          </div>
          <div class="mt-3">
            <div class="flex">
              <label>{{ $t("app.basis.configuration.portal-meta-keywords") }}</label>
              <template v-if="validate.PortalMetaKeywords.$error">
                <div
                  v-for="(error, index) in validate.PortalMetaKeywords.$errors"
                  :key="index"
                  class="text-danger ml-auto"
                >
                  {{ error.$message }}
                </div>
              </template>
            </div>
            <div class="input-group">
              <div class="input-group-text"><FontAwesome icon="quote-right" class="w-4" /></div>
              <input
                type="text"
                name="PortalMetaKeywords"
                class="form-control"
                :class="{ 'border-danger': validate.PortalMetaKeywords.$error }"
                placeholder="Type your text here"
                v-model.trim="validate.PortalMetaKeywords.$model"
              />
            </div>
          </div>
          <div class="mt-3">
            <div class="flex">
              <label>{{ $t("app.basis.configuration.portal-meta-description") }}</label>
              <template v-if="validate.PortalMetaDescription.$error">
                <div
                  v-for="(error, index) in validate.PortalMetaDescription.$errors"
                  :key="index"
                  class="text-danger ml-auto"
                >
                  {{ error.$message }}
                </div>
              </template>
            </div>
            <div class="input-group">
              <div class="input-group-text"><FontAwesome icon="quote-right" class="w-4" /></div>
              <input
                type="text"
                name="PortalMetaDescription"
                class="form-control"
                :class="{ 'border-danger': validate.PortalMetaDescription.$error }"
                placeholder="Type your text here"
                v-model.trim="validate.PortalMetaDescription.$model"
              />
            </div>
          </div>
          <div class="mt-3">
            <div class="flex">
              <label>{{ $t("app.basis.configuration.service-phone") }}</label>
              <template v-if="validate.ServicePhone.$error">
                <div
                  v-for="(error, index) in validate.ServicePhone.$errors"
                  :key="index"
                  class="text-danger ml-auto"
                >
                  {{ error.$message }}
                </div>
              </template>
            </div>
            <div class="input-group">
              <div class="input-group-text"><FontAwesome icon="phone" class="w-4" /></div>
              <input
                type="text"
                name="ServicePhone"
                class="form-control"
                :class="{ 'border-danger': validate.ServicePhone.$error }"
                placeholder="Type your phone here"
                v-model.trim="validate.ServicePhone.$model"
              />
            </div>
          </div>
          <div class="mt-3">
            <div class="flex">
              <label>{{ $t("app.basis.configuration.company-fax") }}</label>
              <template v-if="validate.CompanyFax.$error">
                <div
                  v-for="(error, index) in validate.CompanyFax.$errors"
                  :key="index"
                  class="text-danger ml-auto"
                >
                  {{ error.$message }}
                </div>
              </template>
            </div>
            <div class="input-group">
              <div class="input-group-text"><FontAwesome icon="fax" class="w-4" /></div>
              <input
                type="text"
                name="CompanyFax"
                class="form-control"
                :class="{ 'border-danger': validate.CompanyFax.$error }"
                placeholder="Type your phone here"
                v-model.trim="validate.CompanyFax.$model"
              />
            </div>
          </div>
          <div class="mt-3">
            <div class="flex">
              <label>{{ $t("app.basis.configuration.company-address") }}</label>
              <template v-if="validate.CompanyAddress.$error">
                <div
                  v-for="(error, index) in validate.CompanyAddress.$errors"
                  :key="index"
                  class="text-danger ml-auto"
                >
                  {{ error.$message }}
                </div>
              </template>
            </div>
            <div class="input-group">
              <div class="input-group-text"><FontAwesome type="fas" icon="address-card" class="w-4 pt-1" /></div>
              <input
                type="text"
                name="CompanyFax"
                class="form-control"
                :class="{ 'border-danger': validate.CompanyAddress.$error }"
                placeholder="Type your address here"
                v-model.trim="validate.CompanyAddress.$model"
              />
            </div>
          </div>
          <div class="mt-3">
            <div class="flex">
              <label>{{ $t("app.basis.configuration.service-email") }}</label>
              <template v-if="validate.ServiceEmail.$error">
                <div
                  v-for="(error, index) in validate.ServiceEmail.$errors"
                  :key="index"
                  class="text-danger ml-auto"
                >
                  {{ error.$message }}
                </div>
              </template>
            </div>
            <div class="input-group">
              <div class="input-group-text"><FontAwesome icon="at" class="w-4" /></div>
              <input
                type="text"
                name="ServiceEmail"
                class="form-control"
                :class="{ 'border-danger': validate.ServiceEmail.$error }"
                placeholder="Type your email here"
                v-model.trim="validate.ServiceEmail.$model"
              />
            </div>
          </div>
        </div>
      </div>
      <div class="col-span-12 lg:col-span-6">
        <div class="intro-y box p-5 h-full">
          <div>
            <div class="flex">
              <label>{{ $t("app.basis.configuration.facebook") }}</label>
              <template v-if="validate.Facebook.$error">
                <div
                  v-for="(error, index) in validate.Facebook.$errors"
                  :key="index"
                  class="text-danger ml-auto"
                >
                  {{ error.$message }}
                </div>
              </template>
            </div>
            <div class="input-group">
              <div class="input-group-text"><FontAwesome icon="facebook" class="w-4" /></div>
              <input
                type="text"
                name="Facebook"
                class="form-control"
                :class="{ 'border-danger': validate.Facebook.$error }"
                placeholder="Type your URL here"
                v-model.trim="validate.Facebook.$model"
              />
            </div>
          </div>
          <div class="mt-3">
            <div class="flex">
              <label>{{ $t("app.basis.configuration.instagram") }}</label>
              <template v-if="validate.Instagram.$error">
                <div
                  v-for="(error, index) in validate.Instagram.$errors"
                  :key="index"
                  class="text-danger ml-auto"
                >
                  {{ error.$message }}
                </div>
              </template>
            </div>
            <div class="input-group">
              <div class="input-group-text"><FontAwesome icon="instagram" class="w-4" /></div>
              <input
                type="text"
                name="Instagram"
                class="form-control"
                :class="{ 'border-danger': validate.Instagram.$error }"
                placeholder="Type your URL here"
                v-model.trim="validate.Instagram.$model"
              />
            </div>
          </div>
          <div class="mt-3">
            <div class="flex">
              <label>{{ $t("app.basis.configuration.twitter") }}</label>
              <template v-if="validate.Twitter.$error">
                <div
                  v-for="(error, index) in validate.Twitter.$errors"
                  :key="index"
                  class="text-danger ml-auto"
                >
                  {{ error.$message }}
                </div>
              </template>
            </div>
            <div class="input-group">
              <div class="input-group-text"><FontAwesome icon="twitter" class="w-4" /></div>
              <input
                type="text"
                name="Twitter"
                class="form-control"
                :class="{ 'border-danger': validate.Twitter.$error }"
                placeholder="Type your URL here"
                v-model.trim="validate.Twitter.$model"
              />
            </div>
          </div>
          <div class="mt-3">
            <div class="flex">
              <label>LINE</label>
              <template v-if="validate.LINE.$error">
                <div
                  v-for="(error, index) in validate.LINE.$errors"
                  :key="index"
                  class="text-danger ml-auto"
                >
                  {{ error.$message }}
                </div>
              </template>
            </div>
            <div class="input-group">
              <div class="input-group-text"><FontAwesome icon="line" class="w-4" /></div>
              <input
                type="text"
                name="LINE"
                class="form-control"
                :class="{ 'border-danger': validate.LINE.$error }"
                placeholder="Type your URL here"
                v-model.trim="validate.LINE.$model"
              />
            </div>
          </div>
          <div class="mt-3">
            <label>{{ $t("app.basis.configuration.tracking-code") }}</label>
            <textarea
              v-model.trim="validate.TrackingCode.$model"
              class="form-control w-full border mt-2 h-64"
              :class="{ 'border-danger': validate.TrackingCode.$error }"
              placeholder="Type your script here"
            />
            <template v-if="validate.TrackingCode.$error">
              <div
                v-for="(error, index) in validate.TrackingCode.$errors"
                :key="index"
                class="text-theme-6 mt-2"
              >
                {{ error.$message }}
              </div>
            </template>
          </div>
        </div>
      </div>
      <div class="intro-y col-span-12 lg:col-span-12">
        <div class="text-right">
          <button type="submit" class="btn btn-primary w-24">
            {{ $t("button.save") }}
          </button>
        </div>
      </div>
    </div>
  </form>
</template>

<script setup>
import context, { onMounted, reactive } from "@cloudfun/core";
import { useVuelidate } from "@vuelidate/core";
import { required, email, url } from "@vuelidate/validators";

const application = context.current;
const configuration = reactive({
  AdminTitle: '',
  PortalTitle: '',
  PortalMetaKeywords: '',
  PortalMetaDescription: '',
  ServicePhone: '',
  CompanyFax: '',
  CompanyAddress: '',
  ServiceEmail: '',
  Facebook: '',
  Instagram: '',
  Twitter: '',
  LINE: '',
  TrackingCode: '',
});

const rules = {
  AdminTitle: { required },
  PortalTitle: { required },
  PortalMetaKeywords: { required },
  PortalMetaDescription: { required },
  ServicePhone: { },
  CompanyFax: {},
  CompanyAddress: {},
  ServiceEmail: { email },
  Facebook: { url },
  Instagram: { url },
  Twitter: { url },
  LINE: { url },
  TrackingCode: {},
};

const validate = useVuelidate(rules, configuration);

const save = () => {
  validate.value.$touch();
  if (validate.value.$invalid) {
    context.send('warning', {
      subject: '資料有誤',
      content: '資料驗證失敗，請依提示進行修正後再試一次'
    });
  } else if (application) {
    application.model.dispatch('configuration/update', configuration).then(
      () => { // success
        application.model.dispatch('configuration/read')
        context.send('info', {
          subject: '更新成功',
          content: '系統配置已更新'
        });
      },
      failure => {
        context.send('error', {
          subject: '更新失敗',
          content: failure.message
        });
      }
    )
  }
}

onMounted(() => {
  if (application) application.model.dispatch('configuration/read').then(value => Object.assign(configuration, value));
})

</script>
