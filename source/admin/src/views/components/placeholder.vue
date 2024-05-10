<template>
  <div class="intro-y grid grid-cols-12 gap-6 mt-5">
    <div class="col-span-12 lg:col-span-6">
      <!-- BEGIN: Defult(Auto adaptie the size to parent) -->
      <PreviewComponent class="intro-y box" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Defult(Auto adaptie the size to parent)</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-1"
              >Show example code</label
            >
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview class="h-32 flex justify-center items-center bg-slate-400/50">
            <Placeholder />
          </Preview>
          <Source>
            <Highlight>
              {{ `<Placeholder />` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Defult(Auto adaptie the size to parent) -->
      <!-- BEGIN: Placeholder Text -->
      <PreviewComponent class="intro-y box" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Placeholder Text</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-1"
              >Show example code</label
            >
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview class="h-32 flex justify-center items-center bg-slate-400/50">
            <Placeholder text="Put your text here" />
          </Preview>
          <Source>
            <Highlight>
              {{ `<Placeholder text="Put your text here" />` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Placeholder Text -->
      <!-- BEGIN: Limit Width and Heigt -->
      <PreviewComponent class="intro-y box" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Limited Width and Heigt</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-1"
              >Show example code</label
            >
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview class="h-32 flex justify-center items-center bg-slate-400/50">
            <Placeholder :width="1280" :height="320" />
          </Preview>
          <Source>
            <Highlight>
              {{ `<Placeholder :width="1280" :height="320" />` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Limit Width and Heigt -->
      <!-- BEGIN: Limit Width only -->
      <PreviewComponent class="intro-y box" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Limited Width only</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-1"
              >Show example code</label
            >
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview class="h-32 flex justify-center items-center bg-slate-400/50">
            <Placeholder :width="1280" />
          </Preview>
          <Source>
            <Highlight>
              {{ `<Placeholder :width="1280" />` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Limit Width only -->
      <!-- BEGIN: Limit Height only -->
      <PreviewComponent class="intro-y box" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Limited Height only</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-1"
              >Show example code</label
            >
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview class="h-32 flex justify-center items-center bg-slate-400/50">
            <Placeholder :height="320" />
          </Preview>
          <Source>
            <Highlight>
              {{ `<Placeholder :height="320" />` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Limit Height only -->
      <!-- BEGIN: Unlimited Character -->
      <PreviewComponent class="intro-y box" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Unlimited Character</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-1"
              >Show example code</label
            >
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview class="h-32 flex justify-center items-center bg-slate-400/50">
            <Placeholder :height="320" :unlimitedChar="'W'" />
          </Preview>
          <Source>
            <Highlight>
              {{ `<Placeholder :height="320" :unlimitedChar="'W'" />` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Unlimited Character -->
    </div>
    <div class="col-span-12 lg:col-span-6">
      <div class="intro-y col-span-12 lg:col-span-6">
        <!-- BEGIN: Component API -->
        <div class="intro-y box">
          <div
            class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
          >
            <h2 class="font-medium text-base mr-auto">Component API</h2>
          </div>
          <div class="p-5">
            <table class="table">
              <thead>
                <tr>
                  <th class="whitespace-nowrap w-24">Prop</th>
                  <th class="whitespace-nowrap">Types</th>
                  <th class="whitespace-nowrap">Default</th>
                  <th class="whitespace-nowrap">Description</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td class="align-top text-red-500">`width`</td>
                  <td class="align-top">Number</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    寬度，未指定時自適應父元件寬度，顯示寬度不會超過指定寬度。同時設定寬高將維持比例進行縮放
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`height`</td>
                  <td class="align-top">Number</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    高度，未指定時自適應父高件高度，顯示高度不會超過指定高度。同時設定寬高將維持比例進行縮放
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`text`</td>
                  <td class="align-top">String</td>
                  <td class="align-top">Image</td>
                  <td class="align-top">
                    顯示文字
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`ulimitedChar`</td>
                  <td class="align-top">
                    String | { width: string, height: string }
                  </td>
                  <td class="align-top">∞</td>
                  <td class="align-top">
                    未指定寬度或高度時顯示的字元，可各別指定
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        <!-- END: Component API -->
      </div>
    </div>
  </div>
</template>

<script setup>
import "@cloudfun/placeholder/dist/placeholder.css"
import Placeholder from '@cloudfun/placeholder';
</script>