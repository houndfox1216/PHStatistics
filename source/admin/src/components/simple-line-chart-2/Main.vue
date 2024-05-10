<template>
  <Chart
    type="line"
    :width="width"
    :height="height"
    :data="data"
    :options="options"
  />
</template>

<script setup>
import context, { computed } from "@cloudfun/core";
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
  lineColor: {
    type: String,
    default: "",
  },
});

const model = context.current.model;
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
        label: "# of Votes",
        data: [0, 200, 250, 200, 500, 450, 850, 1050, 950, 1100, 900, 1200],
        borderWidth: 2,
        borderDash: [2, 2],
        borderColor:
          colorScheme.value && props.lineColor.length
            ? props.lineColor
            : colors.slate["300"](),
        backgroundColor: "transparent",
        pointBorderColor: "transparent",
      },
    ],
  };
});

const options = computed(() => {
  return {
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false,
      },
    },
    scales: {
      x: {
        ticks: {
          display: false,
        },
        grid: {
          display: false,
          drawBorder: false,
        },
      },
      y: {
        ticks: {
          display: false,
        },
        grid: {
          display: false,
          drawBorder: false,
        },
      },
    },
  };
});
</script>
