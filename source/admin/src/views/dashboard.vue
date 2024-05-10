<template>
  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-12">
      <div class="grid grid-cols-12 gap-6">
        <!-- BEGIN: General Report -->
        <div class="col-span-12 mt-8">
          <div class="intro-y flex items-center h-10">
            <h2 class="text-lg font-medium truncate mr-5">{{ $t("app.dashboard.general-report") }}</h2>
          </div>
          <div class="grid grid-cols-12 gap-6 mt-5">
            <div class="col-span-12 sm:col-span-6 xl:col-span-4 intro-y">
              <div class="report-box zoom-in">
                <div class="box p-5">
                  <div class="flex">
                    <FontAwesome icon="user" class="text-primary w-14 h-14 mr-4" />
                    <div class="ml-auto">
                      <Tippy
                        tag="div"
                        class="report-box__indicator bg-success cursor-pointer"
                        content="New Users"
                      >
                        {{ $utils.formatMoney(gaData.NewUsers) }}
                        <ChevronUpIcon class="w-4 h-4" />
                      </Tippy>
                    </div>
                    <div class="w-full">
                      <div class="text-3xl font-medium leading-8 text-right">{{ $utils.formatMoney(gaData.Users) }}</div>
                      <div class="text-base text-slate-500 mt-1 text-right">Users</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div class="col-span-12 sm:col-span-6 xl:col-span-4 intro-y">
              <div class="report-box zoom-in">
                <div class="box p-5">
                  <div class="flex">
                    <FontAwesome icon="eye" class="text-warning w-14 h-14 mr-4" />
                    <div class="w-full">
                      <div class="text-3xl font-medium leading-8 text-right">{{ $utils.formatMoney(gaData.Pageviews) }}</div>
                      <div class="text-base text-slate-500 mt-1 text-right">Pageviews</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div class="col-span-12 sm:col-span-6 xl:col-span-4 intro-y">
              <div class="report-box zoom-in">
                <div class="box p-5">
                  <div class="flex">
                    <FontAwesome icon="external-link-alt" class="text-danger w-14 h-14 mr-4" />
                    <div class="w-full">
                      <div class="text-3xl font-medium leading-8 text-right">{{ $utils.formatMoney(gaData.BounceRate, 4) }}%</div>
                      <div class="text-base text-slate-500 mt-1 text-right">Bounce Rate</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div class="col-span-12 sm:col-span-6 xl:col-span-4 intro-y">
              <div class="report-box zoom-in">
                <div class="box p-5">
                  <div class="flex">
                    <FontAwesome icon="link" class="text-pending w-14 h-14 mr-4" />
                    <div class="w-full">
                      <div class="text-3xl font-medium leading-8 text-right">{{ $utils.formatMoney(gaData.Sessions) }}</div>
                      <div class="text-base text-slate-500 mt-1 text-right">Sessions</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div class="col-span-12 sm:col-span-6 xl:col-span-4 intro-y">
              <div class="report-box zoom-in">
                <div class="box p-5">
                  <div class="flex">
                    <FontAwesome icon="file" class="text-success w-14 h-14 mr-4" />
                    <div class="w-full">
                      <div class="text-3xl font-medium leading-8 text-right">{{ $utils.formatMoney(gaData.PageviewsPerSession, 4) }}</div>
                      <div class="text-base text-slate-500 mt-1 text-right">Pageviews Per Session</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div class="col-span-12 sm:col-span-6 xl:col-span-4 intro-y">
              <div class="report-box zoom-in">
                <div class="box p-5">
                  <div class="flex">
                    <FontAwesome icon="clock" class="text-info w-14 h-14 mr-4" />
                    <div class="w-full">
                      <div class="text-3xl font-medium leading-8 text-right">{{ $utils.formatMoney(gaData.AvgSessionDuration, 4) }}</div>
                      <div class="text-base text-slate-500 mt-1 text-right">Avg Session Duration</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <!-- END: General Report -->
        <!-- BEGIN: Active Users Report -->
        <div class="col-span-12 mt-8">
          <div class="intro-y block sm:flex items-center h-10">
            <h2 class="text-lg font-medium truncate mr-5">{{ $t("app.dashboard.active-users-report") }}</h2>
          </div>
          <div class="intro-y box p-5 mt-12 sm:mt-5">
            <div class="report-chart">
              <Chart
                type="line"
                :height="350"
                :data="activeUsersReport.data"
                :options="activeUsersReport.options"
              />
            </div>
          </div>
        </div>
        <!-- END: Active Users Report -->
        <!-- BEGIN: Device Category Chart -->
        <div class="col-span-12 sm:col-span-6 lg:col-span-3 mt-8">
          <div class="intro-y flex items-center h-10">
            <h2 class="text-lg font-medium truncate mr-5">{{ $t("app.dashboard.device-category-chart") }}</h2>
          </div>
          <div class="intro-y box p-5 mt-5">
            <div class="mt-3">
              <Chart
                type="pie"
                :height="222"
                :data="deviceCategoryChart.data"
                :options="deviceCategoryChart.options"
              />
            </div>
            <div class="w-52 sm:w-auto mx-auto mt-8">
              <div v-for="(value, index) in deviceCategoryChart.data.datasets[0].data.slice(0,3)" class="flex items-center mt-3">
                <div :class="`w-2 h-2 bg-${deviceCategoryChart.top3Colors[index]} rounded-full mr-3`"></div>
                <span class="truncate mr-2">{{deviceCategoryChart.data.labels[index]}}</span>
                <span class="font-medium ml-auto">{{(value / gaData.Users * 100).toFixed(2)}}%</span>
              </div>
            </div>
          </div>
        </div>
        <!-- END: Device Category Chart -->
        <!-- BEGIN: Browser Chart -->
        <div class="col-span-12 sm:col-span-6 lg:col-span-3 mt-8">
          <div class="intro-y flex items-center h-10">
            <h2 class="text-lg font-medium truncate mr-5">{{ $t("app.dashboard.browser-chart") }}</h2>
          </div>
          <div class="intro-y box p-5 mt-5">
            <div class="mt-3">
              <Chart
                type="doughnut"
                :height="222"
                :data="browserChart.data"
                :options="browserChart.options"
              />
            </div>
            <div class="w-52 sm:w-auto mx-auto mt-8">
              <div v-for="(value, index) in browserChart.data.datasets[0].data.slice(0,3)" class="flex items-center mt-3">
                <div :class="`w-2 h-2 bg-${browserChart.top3Colors[index]} rounded-full mr-3`"></div>
                <span class="truncate mr-2">{{browserChart.data.labels[index]}}</span>
                <span class="font-medium ml-auto">{{(value / gaData.Users * 100).toFixed(2)}}%</span>
              </div>
            </div>
          </div>
        </div>
        <!-- END: Browser Chart -->
        <!-- BEGIN: Country Chart -->
        <div class="col-span-12 sm:col-span-6 lg:col-span-3 mt-8">
          <div class="intro-y flex items-center h-10">
            <h2 class="text-lg font-medium truncate mr-5">{{ $t("app.dashboard.country-chart") }}</h2>
          </div>
          <div class="intro-y box p-5 mt-5">
            <div class="mt-3">
              <Chart
                type="pie"
                :height="222"
                :data="countryChart.data"
                :options="countryChart.options"
              />
            </div>
            <div class="w-52 sm:w-auto mx-auto mt-8">
              <div v-for="(value, index) in countryChart.data.datasets[0].data.slice(0,3)" class="flex items-center mt-3">
                <div :class="`w-2 h-2 bg-${countryChart.top3Colors[index]} rounded-full mr-3`"></div>
                <span class="truncate mr-2">{{countryChart.data.labels[index]}}</span>
                <span class="font-medium ml-auto">{{(value / gaData.Users * 100).toFixed(2)}}%</span>
              </div>
            </div>
          </div>
        </div>
        <!-- END: Country Chart -->
        <!-- BEGIN: Language Chart -->
        <div class="col-span-12 sm:col-span-6 lg:col-span-3 mt-8">
          <div class="intro-y flex items-center h-10">
            <h2 class="text-lg font-medium truncate mr-5">{{ $t("app.dashboard.language-chart") }}</h2>
          </div>
          <div class="intro-y box p-5 mt-5">
            <div class="mt-3">
              <Chart
                type="doughnut"
                :height="222"
                :data="languageChart.data"
                :options="languageChart.options"
              />
            </div>
            <div class="w-52 sm:w-auto mx-auto mt-8">
              <div v-for="(value, index) in languageChart.data.datasets[0].data.slice(0,3)" class="flex items-center mt-3">
                <div :class="`w-2 h-2 bg-${languageChart.top3Colors[index]} rounded-full mr-3`"></div>
                <span class="truncate mr-2">{{languageChart.data.labels[index]}}</span>
                <span class="font-medium ml-auto">{{(value / gaData.Users * 100).toFixed(2)}}%</span>
              </div>
            </div>
          </div>
        </div>
        <!-- END: Language Chart -->
      </div>
    </div>
  </div>
</template>

<script setup>
import context, { ref, computed, onMounted } from "@cloudfun/core";
import { colors } from "@/utils/colors";

const model = context.current.model;
const darkMode = computed(() => model.getters["midone/darkMode"]);
const colorScheme = computed(() => model.getters["midone/colorScheme"]);

const chartColors = () => [
  colors.primary(0.9),
  colors.success(0.9),
  colors.warning(0.9),
  colors.pending(0.9),
  colors.danger(0.9),
  colors.info(0.9),
  colors.secondary(0.9),
];

const gaData = ref({
  UsersByDate: [],
  NewUsersByDate: [],
  UsersByDeviceCategory: [],
  UsersByBrowser: [],
  UsersByCountry: [],
  UsersByLanguage: [],
});

var activeUsersReport = computed(() => {
  return {
    data: {
      labels: gaData.value.UsersByDate.map(e => { return e.Name.slice(5) }),
      datasets: [
        {
          label: "Users",
          data: gaData.value.UsersByDate.map(e => { return e.Value }),
          borderWidth: 2,
          borderColor: colorScheme.value ? colors.primary() : "",
          backgroundColor: "transparent",
          pointBorderColor: "transparent",
          tension: 0.4,
        },
        {
          label: "New Users",
          data: gaData.value.NewUsersByDate.map(e => { return e.Value }),
          borderDash: [2, 2],
          borderColor: darkMode.value ? colors.slate["400"](0.6) : colors.slate["400"](),
          backgroundColor: "transparent",
          pointBorderColor: "transparent",
          tension: 0.4,
        },
      ],
    },
    options: {
      maintainAspectRatio: false,
      plugins: {
        legend: { labels: { color: colors.slate["500"](0.8) } },
      },
      scales: {
        x: {
          ticks: { font: { size: 12 }, color: colors.slate["500"](0.8) },
          grid: { display: true, drawBorder: false },
        },
        y: {
          ticks: { font: { size: 12 }, color: colors.slate["500"](0.8) },
          grid: {
            color: darkMode.value ? colors.slate["500"](0.3) : colors.slate["300"](),
            borderDash: [2, 2],
            drawBorder: false,
          },
        },
      },
    }
  };
});

var deviceCategoryChart = computed(() => {
  return {
    top3Colors: [ "primary", "success", "warning" ],
    data: {
      labels: gaData.value.UsersByDeviceCategory.map(e => { return e.Name }),
      datasets: [
        {
          data: gaData.value.UsersByDeviceCategory.map(e => { return e.Value }),
          backgroundColor: colorScheme.value ? chartColors() : "",
          hoverBackgroundColor: colorScheme.value ? chartColors() : "",
          borderWidth: 2,
          borderColor: darkMode.value ? colors.darkmode[700]() : colors.white,
        }
      ],
    },
    options: {
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: false,
        },
      },
    }
  };
});

var browserChart = computed(() => {
  return {
    top3Colors: [ "primary", "success", "warning" ],
    data: {
      labels: gaData.value.UsersByBrowser.map(e => { return e.Name }),
      datasets: [
        {
          data: gaData.value.UsersByBrowser.map(e => { return e.Value }),
          backgroundColor: colorScheme.value ? chartColors() : "",
          hoverBackgroundColor: colorScheme.value ? chartColors() : "",
          borderWidth: 2,
          borderColor: darkMode.value ? colors.darkmode[700]() : colors.white,
        }
      ],
    },
    options: {
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: false,
        },
      },
    }
  };
});

var countryChart = computed(() => {
  return {
    top3Colors: [ "primary", "success", "warning" ],
    data: {
      labels: gaData.value.UsersByCountry.map(e => { return e.Name }),
      datasets: [
        {
          data: gaData.value.UsersByCountry.map(e => { return e.Value }),
          backgroundColor: colorScheme.value ? chartColors() : "",
          hoverBackgroundColor: colorScheme.value ? chartColors() : "",
          borderWidth: 2,
          borderColor: darkMode.value ? colors.darkmode[700]() : colors.white,
        }
      ],
    },
    options: {
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: false,
        },
      },
    }
  };
});

var languageChart = computed(() => {
  return {
    top3Colors: [ "primary", "success", "warning" ],
    data: {
      labels: gaData.value.UsersByLanguage.map(e => { return e.Name }),
      datasets: [
        {
          data: gaData.value.UsersByLanguage.map(e => { return e.Value }),
          backgroundColor: colorScheme.value ? chartColors() : "",
          hoverBackgroundColor: colorScheme.value ? chartColors() : "",
          borderWidth: 2,
          borderColor: darkMode.value ? colors.darkmode[700]() : colors.white,
        }
      ],
    },
    options: {
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: false,
        },
      },
    }
  };
});

onMounted(() => {
  model.clients.unauthorized.get("System/GoogleAnalytics").then(
    ({ payload }) => {
      Object.assign(gaData.value, payload)
      if (gaData.value.UsersByBrowser.length > 7) {
        gaData.value.UsersByBrowser[6].Name = "Other";
        gaData.value.UsersByBrowser[6].Value = gaData.value.UsersByBrowser.slice(6).reduce((sum, e) => sum + e.Value, 0);
        gaData.value.UsersByBrowser = gaData.value.UsersByBrowser.slice(0, 7);
      }
      if (gaData.value.UsersByCountry.length > 7) {
        gaData.value.UsersByCountry[6].Name = "Other";
        gaData.value.UsersByCountry[6].Value = gaData.value.UsersByCountry.slice(6).reduce((sum, e) => sum + e.Value, 0);
        gaData.value.UsersByCountry = gaData.value.UsersByCountry.slice(0, 7);
      }
      if (gaData.value.UsersByLanguage.length > 7) {
        gaData.value.UsersByLanguage[6].Name = "Other";
        gaData.value.UsersByLanguage[6].Value = gaData.value.UsersByLanguage.slice(6).reduce((sum, e) => sum + e.Value, 0);
        gaData.value.UsersByLanguage = gaData.value.UsersByLanguage.slice(0, 7);
      }
    }
  );
});
</script>
