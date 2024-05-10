import { ISitemapNode } from '@cloudfun/core'

const nodes: (string | ISitemapNode)[] = [
  { icon: 'fa-film', to: 'media-file', title: 'app.streaming.media-file.title' },
  { icon: 'fa-video', to: 'live-source', title: 'app.streaming.live-source.title' },
];

export default nodes;
