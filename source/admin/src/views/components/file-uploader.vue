<template>
  <div class="intro-y grid grid-cols-12 gap-6 mt-5">
    <div class="col-span-12 lg:col-span-6">
      <!-- BEGIN: Defult -->
      <PreviewComponent class="intro-y box" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Defult</h2>
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
            <FileUploader 
              :action="uploadAction"
              v-model="demoData"
              @error="(message) => $send('error', message)"
            />
          </Preview>
          <Source>
            <Highlight>
              {{ `
<template>
  <FileUploader
    action="xxxx.xxxx.xxxx.xxxx"
    @error="(message) => $send('error', message)"
  />
</template>
<script setup>
  import "@cloudfun/file-uploader/dist/file-uploader.scss";
  import FileUploader from '@cloudfun/file-uploader';
</script>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Defult -->
      <!-- BEGIN: Image mode -->
      <PreviewComponent class="intro-y box mt-5" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">Image mode</h2>
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
            <FileUploader
              mode="image"
              :limitedWidth="1280"
              :limited-height="320"
              :preview-height="150"
              :action="uploadAction"
              v-model="demoData"
              @error="(message) => $send('error', message)"
            />
          </Preview>
          <Source>
            <Highlight>
              {{ `
<template>
  <FileUploader
    mode="image"
    action="xxxx.xxxx.xxxx.xxxx"
    :limitedWidth="1280"
    :limited-height="320"
    :preview-height="150"
    @error="(message) => $send('error', message)"
  />
</template>
<script setup>
  import "@cloudfun/file-uploader/dist/file-uploader.scss";
  import FileUploader from '@cloudfun/file-uploader';
</script>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: Image mode -->
      <!-- BEGIN: List mode + default image -->
      <PreviewComponent class="intro-y box mt-5" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">List mode + default image</h2>
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
            <FileUploader
              mode="list"
              default-image="/images/avatar.svg"
              :timeout="30000"
              :multiple="true"
              :preview-height="100"
              :action="uploadAction"
              v-model="demoData"
              @error="(message) => $send('error', message)"
            />
          </Preview>
          <Source>
            <Highlight>
              {{ `
<template>
  <FileUploader
    mode="list"
    action="xxxx.xxxx.xxxx.xxxx"
    default-image="/images/midone.svg"
    :timeout="30000"
    :multiple="true"
    :preview-height="100"
    @error="(message) => $send('error', message)"
  />
</template>
<script setup>
  import "@cloudfun/file-uploader/dist/file-uploader.scss";
  import FileUploader from '@cloudfun/file-uploader';
</script>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: List mode + default image -->
      <!-- BEGIN: List mode + flex statistic -->
      <PreviewComponent class="intro-y box mt-5" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">List mode + flex statistic</h2>
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
            <FileUploader 
              mode="list" 
              :multiple="true"
              :display="{ statistic: 'flex' }"
              :action="uploadAction"
              v-model="demoData"
              @error="(message) => $send('error', message)"
            />
          </Preview>
          <Source>
            <Highlight>
              {{ `
<template>
  <FileUploader 
    mode="list" 
    action="xxxx.xxxx.xxxx.xxxx"
    :multiple="true"
    :display="{ statistic: 'flex' }"
    @error="(message) => $send('error', message)"
  />
</template>
<script setup>
  import "@cloudfun/file-uploader/dist/file-uploader.scss";
  import FileUploader from '@cloudfun/file-uploader';
</script>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: List mode + flex statistic -->
      <!-- BEGIN: List mode + Batch Upload -->
      <PreviewComponent class="intro-y box mt-5" v-slot="{ toggle }">
        <div
          class="flex flex-col sm:flex-row items-center p-5 border-b border-slate-200/60 dark:border-darkmode-400"
        >
          <h2 class="font-medium text-base mr-auto">List mode + Batch Upload(Maximum 3 requests)</h2>
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
            <FileUploader
              mode="list"
              :timeout="30000"
              :multiple="true"
              :preview-height="100"
              :batch-uploader="batchUploader"
              :action="uploadAction"
              @error="(message) => this.$send('error', message)"
            />
          </Preview>
          <Source>
            <Highlight>
              {{ `
<template>
  <FileUploader
    mode="list"
    action="xxxx.xxxx.xxxx.xxxx"
    :timeout="30000"
    :multiple="true"
    :preview-height="100"
    :batch-uploader="batchUploader"
    @error="(message) => this.$send('error', message)"
  />
</template>
<script setup>
  import "@cloudfun/file-uploader/dist/file-uploader.scss";
  import FileUploader, { BatchUploader } from '@cloudfun/file-uploader';
  const batchUploader = new BatchUploader(ref([]));
  onMounted(() => {
    batchUploader.start();
  });
  onUnmounted(() => {
    batchUploader.stop();
  })
</script>
              ` }}
            </Highlight>
          </Source>
        </div>
      </PreviewComponent>
      <!-- END: List mode + Batch Upload -->
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
                  <td class="align-top">UploadFile[]</td>
                  <td class="align-top">[]</td>
                  <td class="align-top">
                    由 v-model 綁定的值
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`name`</td>
                  <td class="align-top">String</td>
                  <td class="align-top">"files"</td>
                  <td class="align-top">
                    Input Element 名稱，當提交到後端時可用於表單資料索引
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`accept`</td>
                  <td class="align-top">String</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    可接受的類型，可以 mimeType 開頭或附檔名結尾並以逗號隔開。ex: "image/*,*.doc"
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`multiple`</td>
                  <td class="align-top">Boolean</td>
                  <td class="align-top">false</td>
                  <td class="align-top">
                    是否允許多個檔案
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`allowdirs`</td>
                  <td class="align-top">Boolean</td>
                  <td class="align-top">false</td>
                  <td class="align-top">
                    僅供Firefox使用，用於相容於舊版或特殊瀏覽器
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`directory`</td>
                  <td class="align-top">Boolean</td>
                  <td class="align-top">false</td>
                  <td class="align-top">
                    僅供Webkit使用，用於以目錄為選取項目(無法單選檔案)
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`disabled`</td>
                  <td class="align-top">Boolean</td>
                  <td class="align-top">false</td>
                  <td class="align-top">
                    禁用此元件，純顯示用
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`title`</td>
                  <td class="align-top">String</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    input 的 title 屬性，僅於 input 模式使用
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`placeholder`</td>
                  <td class="align-top">String</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    當無任何檔案時所顯示的文字
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`droppable`</td>
                  <td class="align-top">Boolean</td>
                  <td class="align-top">true</td>
                  <td class="align-top">
                    是否允許拖曳檔案至元件
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`mode`</td>
                  <td class="align-top">"image" | "input" | "list"</td>
                  <td class="align-top">"input"</td>
                  <td class="align-top">
                    呈現元件的模式，分別有 input(輸入模式)、image(圖片模式)、list(列表模式)
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`display`</td>
                  <td class="align-top">{ preview?: boolean, input?: boolean, statistic?: "list" | "flex" }</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    各模式有預設的顯示方式，可於此進行自訂
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`previewHeight`</td>
                  <td class="align-top">Number</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    預覽區域的高度，用於自適應高度時強制撐開固定的高度
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`limitedWidth`</td>
                  <td class="align-top">Number</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    限定寬度，僅用於圖片，當圖片的寬度與設定不同時，將自動進行縮放
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`limitedHeight`</td>
                  <td class="align-top">Number</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    限定高度，僅用於圖片，當圖片的高度與設定不同時，將自動進行縮放
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`maximumSize`</td>
                  <td class="align-top">Number</td>
                  <td class="align-top">0</td>
                  <td class="align-top">
                    最大檔案容量，0為不限制，當檔案超過此容量，將會發出 error event
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`maximumCount`</td>
                  <td class="align-top">Number</td>
                  <td class="align-top">0</td>
                  <td class="align-top">
                    最大檔案數量，0為不限制，當檔案數量超過此數值，將會發出 error event
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`timeout`</td>
                  <td class="align-top">Number</td>
                  <td class="align-top">0</td>
                  <td class="align-top">
                    逾時時間，0為不限制，當上傳請求逾時未回應時，將會發出 error event
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`action`</td>
                  <td class="align-top">String</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    進行上傳時，後端接受檔案的URL，預設為當前URL
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`headers`</td>
                  <td class="align-top">Record&lt;string,string&gt;</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    上傳請求的自訂 Headers
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`parameters`</td>
                  <td class="align-top">Record&lt;string,string&gt;</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    上傳請求的自訂參數
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`defaultImage`</td>
                  <td class="align-top">string</td>
                  <td class="align-top"></td>
                  <td class="align-top">
                    未選擇檔案時的預設圖片
                  </td>
                </tr>
              </tbody>
            </table>
            <table class="table mt-5">
              <thead>
                <tr>
                  <th class="whitespace-nowrap w-24">Event</th>
                  <th class="whitespace-nowrap">Returns</th>
                  <th class="whitespace-nowrap">Description</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td class="align-top text-red-500">`change`</td>
                  <td class="align-top">
                    UploadFile[]
                  </td>
                  <td class="align-top">
                    當項目的狀態改變時回傳所有檔案資料
                  </td>
                </tr>
                <tr>
                  <td class="align-top text-red-500">`error`</td>
                  <td class="align-top">
                    string, "error" | "timeout": "abort"
                  </td>
                  <td class="align-top">
                    當發生錯誤時回傳訊息與類型
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
import CloudFun, { onMounted } from "@cloudfun/core";
import { faker } from "@/utils/faker";
import { onUnmounted } from "vue";

import "@cloudfun/file-uploader/dist/file-uploader.scss";
import FileUploader from '@cloudfun/file-uploader';

const demoData = [
  { id: 0, name: faker[0].images[0].split('/').pop(), url: faker[0].images[0], mimeType: 'image/png', status: 'success', progress: 100 },
  { id: 1, name: faker[1].images[0].split('/').pop(), url: faker[1].images[0], mimeType: 'image/png', status: 'failure', progress: 80, message: 'Failure to upload' },
  { id: 2, name: faker[2].images[0].split('/').pop(), url: faker[2].images[0], mimeType: 'image/png', status: 'active', progress: 70, speed: 999123 },
  { id: 3, name: faker[3].images[0].split('/').pop(), url: faker[3].images[0], mimeType: 'image/png', status: 'pending' },
];
const uploadAction = `${process.env.VITE_SERVICE_URI}/api/file`;
const batchUploader = CloudFun.root.model.state.batchUploader;
onMounted(() => {
  batchUploader.start();
});
onUnmounted(() => {
  batchUploader.stop();
})
</script>