<template>
  <div class="rounded-lg border bg-gray-100 dark:bg-darkmode-700 pt-2">
    <div class="flex items-center justify-between px-8">
      <div class="flex">
        <button v-if="images" type="button" title="新增" @click="add()">
          <FontAwesome
            icon="plus-circle"
            type="fas"
            class="w-4 h-4 text-primary"
          />
        </button>
      </div>
      <span>共 {{ images?.length || 0 }} 項</span>
    </div>
    <div v-if="images?.length" class="container mb-3" :style="{ width: width }">
      <swiper
        observer
        observeParents
        resizeObserver
        grabCursor
        :key="version"
        :mousewheel="mousewheel"
        :navigation="navigation"
        :pagination="pagination ? { clickable: true } : undefined"
        :lazy="lazy"
        :slidesPerView="Math.min(images?.length || 1, pageSize)"
        :spaceBetween="itemSpace"
      >
        <swiper-slide
          class="border-0 rounded-lg"
          v-for="(image, index) in images"
          :key="index"
        >
          <div class="rounded-lg bg-checkerboard">
            <img
              class="rounded-lg relative m-auto h-48 object-scale-down"
              :src="image.Uri"
              @click="edit(image)"
            />
          </div>
          <div class="absolute top-0 right-0 m-auto">
            <button type="button" title="刪除" @click="remove(image)">
              <FontAwesome
                icon="trash-alt"
                type="fas"
                class="w-6 h-6 text-red-500 p-1 rounded-tr-lg rounded-bl-lg bg-white opacity-70"
              />
            </button>
          </div>
        </swiper-slide>
      </swiper>
    </div>
    <div class="text-3xl flex text-center px-4 pb-5" v-else>
      <div v-for="size in pageSize" :key="'default-placeholder-' + size"
        class="mx-1 mb-2 py-20 border rounded-lg bg-checkerboard"
        :class="'w-1/' + pageSize">
        Coming soon
      </div>
    </div>
  </div>
  <slot name="default" v-bind="{ data: editingImage, save, visible: modalVisible, setVisible: (value) => modalVisible = value }" />
</template>

<style scoped>
.swiper-container {
  padding-top: 5px;
  padding-bottom: 30px;
}
</style>

<script>
import { defineComponent, ref } from '@cloudfun/core'

import 'swiper/swiper-bundle.min.css'
import SwiperCore, {
  Navigation,
  Pagination,
  Autoplay,
  Mousewheel
} from 'swiper'
import { Swiper, SwiperSlide } from 'swiper/vue'

SwiperCore.use([Navigation, Pagination, Autoplay, Mousewheel])

export default defineComponent({
  components: {
    Swiper,
    SwiperSlide,
  },
  props: {
    mousewheel: { type: Boolean, default: true },
    navigation: { type: Boolean, dafault: true },
    pagination: { type: Boolean, default: true },
    pageSize: { type: Number, default: 3 },
    itemSpace: { type: Number, default: 3 },
    lazy: { type: Boolean, dafault: true },
    width: String,
    value: Array,
    modelValue: Array,
    onRefresh: Function,
    onAdd: Function,
    onEdit: Function,
    onSave: Function,
    onRemove: Function
  },
  emits: [ "change", "update:modelValue" ],
  setup (props) {
    return {
      form: ref({}),
      images: ref(props.modelValue ?? props.value),
      version: ref(0),
      loading: ref(false),
      editingImage: ref(null),
      modalVisible: ref(false),
    }
  },
  methods: {
    reload(images) {
      const action = () => {
        this.images = images;
        this.$emit('change', this.images);
        this.$emit('update:modelValue', this.images);
      }
      if (this.$props.onRefresh) this.$emit('refresh');
      else action();
    },
    add() {
      this.editingImage = {};
      this.modalVisible = true;
      const action = async () => {
        this.modalVisible = true;
      }
      if (this.$props.onAdd) this.$emit('add', this.editingImage, action);
      else action();
    },
    async edit (image) {
      this.editingImage = image;
      const action = async () => {
        this.modalVisible = true;
      }
      if (this.$props.onEdit) this.$emit('edit', image ?? { Uri: '' }, action);
      else action();
    },
    async save(image) {
      if (!image?.Uri) {
        alert('請選擇上傳的照片');
        return;
      }
      const action = () => {
        this.$emit('change', this.images);
        this.$emit('update:modelValue', this.images);
        this.loading = false;
        this.modalVisible = false;
      }
      this.loading = true;
      if (this.$props.onSave) this.$emit('save', image, action);
      else action();
    },
    remove(image) {
      if (image && confirm('確定要進行刪除嗎?')) {
        const action = () => {
          const index = this.images.indexOf(image)
          if (index >= 0) this.images.splice(index, 1)
          this.$emit('change', this.images);
          this.$emit('update:modelValue', this.images)
        }
        if (this.$props.onRemove) this.$emit('remove', image, action)
        else action()
      }
    }
  },
})
</script>
