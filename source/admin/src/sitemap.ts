import { ISitemapNode } from "@cloudfun/core";
import basisModule from "@/modules/basis/sitemap";
import contentModule from "@/modules/content/sitemap";
import manufactureModule from "@/modules/manufacture/sitemap";
import streamingModule from "@/modules/streaming/sitemap";
import midoneTemplate from "@/midone-template/sitemap";

const sitemap: ISitemapNode = { icon: 'HomeIcon', to: '/', title: 'app.home.title', subNodes: [
  { icon: "fa-dashboard", to: "/dashboard", title: "app.dashboard.title" },
  { icon: 'fa-id-card-clip', title: 'app.profile.title', subNodes: [
    { icon: "fas-user", to: "/profile", title: "app.profile.information.title" },
    { icon: "fa-lock", to: "profile/change-password", title: "app.profile.change-password.title" },
  ]},
  { icon: 'fa-microchip', title: 'app.components.title', subNodes: [
    { icon: "fa-list-check", to: "/components/checkbox-list", title: "app.components.checkbox-list.title" },
    { icon: "fa-wand-magic-sparkles", to: "/components/stepper", title: "app.components.stepper.title" },
    { icon: "SquareIcon", to: "/components/placeholder", title: "app.components.placeholder.title" },
    { icon: "fa-cloud-arrow-up", to: "/components/file-uploader", title: "app.components.file-uploader.title" },
  ]},
  { icon: 'fa-rectangle-list', to: 'information', title: 'app.basis.information.title' },
  "devider",
  { icon: "fa-seedling", title: "app.basis.title", subNodes: basisModule },
  { icon: "fa-display", title: "app.content.title", subNodes: contentModule },
  { icon: "fa-industry", title: "app.manufacture.title", subNodes: manufactureModule },
  { icon: "fa-tower-cell", title: "app.streaming.title", subNodes: streamingModule },
  "devider",
  { icon: '/images/midone.svg', title: 'Midone', subNodes: midoneTemplate },
]};

export default sitemap
