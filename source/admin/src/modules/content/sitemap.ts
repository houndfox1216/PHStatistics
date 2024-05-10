import { ISitemapNode } from '@cloudfun/core'

const nodes: (string | ISitemapNode)[] = [
  { icon: 'fa-rectangle-ad', to: 'banner', title: 'app.content.banner.title' },
  { icon: 'fas-newspaper', to: 'news', title: 'app.content.news.title' },
  { icon: 'fa-link', to: 'url-segment', title: 'app.content.url-segment.title' },
  { icon: 'fa-globe', to: 'page', title: 'app.content.page.title' },
];

export default nodes;
