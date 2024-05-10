<template>
  <Chart
    type="bar"
    :width="width"
    :height="height"
    :data="data"
    :options="options"
  />
</template>

<script setup>
import context, { computed } from "@cloudfun/core";
import { helper as $h } from "@/utils/helper";
import { colors } from "@/utils/colors";

const props = defineProps({
  width: {
    type: [Number, String],
    default: "auto",
  },
  height: {
    type: [Number, String],
    default: "auto",
  },
});

const model = context.current.model;
const darkMode = computed(() => model.getters["midone/darkMode"]);
const colorScheme = computed(() => model.getters["midone/colorScheme"]);

const data = computed(() => {
  return {
    labels: [
      "Jan",
      "Feb",
      "Mar",
      "Apr",
      "May",
      "Jun",
      "Jul",
      "Aug",
      "Sep",
      "Oct",
      "Nov",
      "Dec",
    ],
    datasets: [
      {
        label: "Html Template",
        barPercentage: 0.5,
        barThickness: 6,
        maxBarThickness: 8,
        minBarLength: 2,
        backgroundColor: colorScheme.value ? colors.primary() : "",
        data: $h.randomNumbers(-100, 100, 12),
      },
      {
        label: "VueJs Template",
        barPercentage: 0.5,
        barThickness: 6,
        maxBarThickness: 8,
        minBarLength: 2,
        backgroundColor: darkMode.value
          ? colors.darkmode["200"]()
          : colors.slate["300"](),
        data: $h.randomNumbers(-100, 100, 12),
      },
    ],
  };
});

const options = computed(() => {
  return {
    maintainAspectRatio: false,
    plugins: {
      legend: {
        labels: {
          color: colors.slate["500"](0.8),
        },
      },
    },
    scales: {
      x: {
        stacked: true,
        ticks: {
          font: {
            size: 12,
          },
          color: colors.slate["500"](0.8),
        },
        grid: {
          display: false,
          drawBorder: false,
        },
      },
      y: {
        stacked: true,
        ticks: {
          font: {
            size: 12,
          },
          color: colors.slate["500"](0.8),
          callback: function (value) {
            return "$" + value;
          },
        },
        grid: {
          color: darkMode.value
            ? colors.slate["500"](0.3)
            : colors.slate["300"](),
          borderDash: [2, 2],
          drawBorder: false,
        },
      },
    },
  };
});
</script>
