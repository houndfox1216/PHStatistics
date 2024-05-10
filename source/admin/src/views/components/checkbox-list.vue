<template>
  <div class="intro-y grid grid-cols-12 gap-6 mt-5">
    <div class="col-span-12 lg:col-span-6">
      <!-- BEGIN: Flex Layout -->
      <PreviewComponent class="intro-y box" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Flex Layout</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-1">Show example code</label>
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview>
            <CheckboxList 
              :items="[
                { name: 'Python', value: 1 }, 
                { name: 'JavaScript', value: 2 },
                { name: 'Java', value: 3 }, 
                { name: 'C#', value: 4 }, 
                { name: 'C', value: 5 }, 
                { name: 'C++', value: 6 }, 
                { name: 'Go', value: 7 }, 
                { name: 'R', value: 8 }, 
                { name: 'Swift', value: 9 }, 
                { name: 'PHP', value: 10 }, 
              ]"
              :modelValue="[1,3,4]"
            />
          </Preview>
          <Source>
            <Highlight>
              {{ `
<CheckboxList 
  :items="[
    { name: 'Python', value: 1 }, 
    { name: 'JavaScript', value: 2 },
    { name: 'Java', value: 3 }, 
    { name: 'C#', value: 4 }, 
    { name: 'C', value: 5 }, 
    { name: 'C++', value: 6 }, 
    { name: 'Go', value: 7 }, 
    { name: 'R', value: 8 }, 
    { name: 'Swift', value: 9 }, 
    { name: 'PHP', value: 10 }, 
  ]"
  :modelValue="[1,3,4]"
/>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Flex Layout -->
      <!-- BEGIN: Column Layout -->
      <PreviewComponent class="intro-y box mt-5" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Column Layout</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-1">Show example code</label>
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview>
            <CheckboxList 
              :columnCount="4" 
              :items="[
                { name: 'Python', value: 1 }, 
                { name: 'JavaScript', value: 2 },
                { name: 'Java', value: 3 }, 
                { name: 'C#', value: 4 }, 
                { name: 'C', value: 5 }, 
                { name: 'C++', value: 6 }, 
                { name: 'Go', value: 7 }, 
                { name: 'R', value: 8 }, 
                { name: 'Swift', value: 9 }, 
                { name: 'PHP', value: 10 }, 
              ]"
              :modelValue="[1,3,4,5]"
            />
          </Preview>
          <Source>
            <Highlight>
              {{ `
<CheckboxList 
  :columnCount="4" 
  :items="[
    { name: 'Python', value: 1 }, 
    { name: 'JavaScript', value: 2 },
    { name: 'Java', value: 3 }, 
    { name: 'C#', value: 4 }, 
    { name: 'C', value: 5 }, 
    { name: 'C++', value: 6 }, 
    { name: 'Go', value: 7 }, 
    { name: 'R', value: 8 }, 
    { name: 'Swift', value: 9 }, 
    { name: 'PHP', value: 10 }, 
  ]"
  :modelValue="[1,3,4,5]"
/>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Column Layout -->
      <!-- BEGIN: Checkbox Group  -->
      <PreviewComponent class="intro-y box mt-5" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Checkbox Group</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-2">Show example code</label>
            <input
              @click="toggle"
              class="form-check-input mr-0 ml-3"
              type="checkbox"
            />
          </div>
        </div>
        <div class="p-5">
          <Preview>
            <CheckboxList 
              :columnCount="4" 
              :items="[
                { name: 'Object-oriented languages', items: [{ name: 'Java', value: 3 }, { name: 'C#', value: 4 }, { name: 'C++', value: 6 }] },
                { name: 'Compiled languages', items: [{ name: 'Java', value: 3 }, { name: 'C#', value: 4 }, { name: 'C', value: 5 }, { name: 'C++', value: 6 }, { name: 'Go', value: 7 }, { name: 'Swift', value: 9 }] },
                { name: 'Interpreted languages', items: [{ name: 'Python', value: 1 }, { name: 'JavaScript', value: 2 }, { name: 'PHP', value: 10 }] },
                { name: 'R', value: 8 },
              ]" 
              :modelValue="[1,3,4,5]"
            />
          </Preview>
          <Source>
            <Highlight>
              {{ `
<CheckboxList 
  :columnCount="4" 
  :items="[
    { name: 'Object-oriented languages', items: [{ name: 'Java', value: 3 }, { name: 'C#', value: 4 }, { name: 'C++', value: 6 }] },
    { name: 'Compiled languages', items: [{ name: 'Java', value: 3 }, { name: 'C#', value: 4 }, { name: 'C', value: 5 }, { name: 'C++', value: 6 }, { name: 'Go', value: 7 }, { name: 'Swift', value: 9 }] },
    { name: 'Interpreted languages', items: [{ name: 'Python', value: 1 }, { name: 'JavaScript', value: 2 }, { name: 'PHP', value: 10 }] },
    { name: 'R', value: 8 },
  ]" 
  :modelValue="[1,3,4,5]"
/>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Checkbox Group -->
      <!-- BEGIN: Readonly  -->
      <PreviewComponent class="intro-y box mt-5" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Readonly</h2>
          <div
            class="form-check form-switch w-full sm:w-auto sm:ml-auto mt-3 sm:mt-0"
          >
            <label class="form-check-label ml-0" for="show-example-2"
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
          <Preview>
            <CheckboxList 
              :columnCount="4" 
              :items="[
                { name: 'Object-oriented languages', items: [{ name: 'Java', value: 3 }, { name: 'C#', value: 4 }, { name: 'C++', value: 6 }] },
                { name: 'Compiled languages', items: [{ name: 'Java', value: 3 }, { name: 'C#', value: 4 }, { name: 'C', value: 5 }, { name: 'C++', value: 6 }, { name: 'Go', value: 7 }, { name: 'Swift', value: 9 }] },
                { name: 'Interpreted languages', items: [{ name: 'Python', value: 1 }, { name: 'JavaScript', value: 2 }, { name: 'PHP', value: 10 }] },
                { name: 'R', value: 8 },
              ]" 
              :modelValue="[1,3,4,5]"
              :readonly="true"
            />
          </Preview>
          <Source>
            <Highlight>
              {{ `
<CheckboxList 
  :columnCount="4" 
  :items="[
    { name: 'Object-oriented languages', items: [{ name: 'Java', value: 3 }, { name: 'C#', value: 4 }, { name: 'C++', value: 6 }] },
    { name: 'Compiled languages', items: [{ name: 'Java', value: 3 }, { name: 'C#', value: 4 }, { name: 'C', value: 5 }, { name: 'C++', value: 6 }, { name: 'Go', value: 7 }, { name: 'Swift', value: 9 }] },
    { name: 'Interpreted languages', items: [{ name: 'Python', value: 1 }, { name: 'JavaScript', value: 2 }, { name: 'PHP', value: 10 }] },
    { name: 'R', value: 8 },
  ]" 
  :modelValue="[1,3,4,5]"
  :readonly="true"
/>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Readonly -->
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
                  <td class="align-top text-red-500">`modelValue`</td>
                  <td class="align-top">Array</td>
                  <td class="align-top">[]</td>
                  <td class="align-top">
                    由 v-model 綁定的值
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`columnCount`</td>
                  <td class="align-top">Number</td>
                  <td class="align-top">NaN</td>
                  <td class="align-top">
                    指定的欄位數量，小於 1 表示無欄位(flex)
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`readonly`</td>
                  <td class="align-top">Boolean</td>
                  <td class="align-top">false</td>
                  <td class="align-top">
                    指定是否唯讀
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`items`</td>
                  <td class="align-top">
                    Array&lt;CheckboxListItem | CheckboxListGroupItem&gt;
                  </td>
                  <td class="align-top">[]</td>
                  <td class="align-top">
                    用以呈現的項目。name: 顯示名稱，value: 選項值，items: 群組項目
                  </td>
                </tr>
              </tbody>
            </table>
            <table class="table mt-5">
              <thead>
                <tr>
                  <th class="whitespace-nowrap w-24">Event</th>
                  <th class="whitespace-nowrap">Description</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td class="align-top text-red-500">`change`</td>
                  <td class="align-top">
                    當任何項目的選擇狀態改變時觸法此事件
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
import "@cloudfun/checkbox-list/dist/checkbox-list.scss";
import CheckboxList from '@cloudfun/checkbox-list';
</script>