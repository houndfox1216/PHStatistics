<template>
  <video
    ref="video"
    class="video-js vjs-default-skin"
    :width="width"
    :height="height"
    controls
  >
    <source :src="src" />
  </video>
</template>

<script>
import { ref } from "@cloudfun/core";
import videojs from "video.js";
import "video.js/dist/video-js.css";

export default {
  data() {
    return {
      player: ref(),
    };
  },
  props: {
    width: Number,
    height: Number,
    src: String,
  },
  mounted() {
    this.player = videojs(this.$refs.video);
  },
};
</script>