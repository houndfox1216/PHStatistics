<template>
  <div
    :style="{
      width: `${width}px`,
      height: `${height}px`,
    }"
  >
    <canvas ref="chartRef" :class="class"></canvas>
  </div>
</template>

<script setup>
import { onMounted, ref, inject, watch } from "vue";
import dom from "@left4code/tw-starter/dist/js/dom";
import { helper as $h } from "@/utils/helper";
import Chart from "chart.js/auto";

const props = defineProps({
  type: {
    type: String,
    required: true,
    default: "line",
    validator: (value) => {
      return ["line", "pie", "doughnut", "bar"].indexOf(value) !== -1;
    },
  },
  data: {
    type: Object,
    required: true,
    default: () => ({}),
  },
  options: {
    type: Object,
    default: () => ({}),
  },
  width: {
    type: [Number, String],
    default: "auto",
  },
  height: {
    type: [Number, String],
    default: "auto",
  },
  refKey: {
    type: String,
    default: null,
  },
  class: {
    type: String,
    default: "",
  },
});

const chartRef = ref();
const init = () => {
  const canvas = chartRef.value?.getContext("2d");
  const chart = new Chart(canvas, {
    type: props.type,
    data: $h.toRaw(props.data),
    options: props.options,
  });

  watch(props, () => {
    chart.data = $h.toRaw(props.data);
    chart.options = props.options;
    chart.update();
  });

  // Attach ChartJs instance
  chartRef.value.instance = chart;
};

// const setSize = () => {
//   if (props.width) {
//     dom(chartRef.value).attr({
//       width: props.width,
//     });
//   }

//   if (props.height) {
//     dom(chartRef.value).attr({
//       height: props.height,
//     });
//   }
// };

const bindInstance = () => {
  if (props.refKey) {
    const bind = inject(`bind[${props.refKey}]`);
    if (bind) {
      bind(chartRef.value);
    }
  }
};

onMounted(() => {
  bindInstance();
  // setSize();
  init();
});
</script>
