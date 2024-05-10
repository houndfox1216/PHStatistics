import { ISitemapNode } from '@cloudfun/core'

const nodes: (string | ISitemapNode)[] = [
  { icon: 'SettingsIcon', to: 'configuration', title: 'app.basis.configuration.title' },
  { icon: 'fa-user-shield', to: 'person', title: 'app.basis.person.title' },
  {
    icon: 'fa-shield-halved',
    title: 'app.basis.permission.title',
    subNodes: [
      { icon: 'fa-wand-sparkles', to: 'permission-wizard', title: 'app.basis.permission.wizard.title' },
      { icon: 'fa-user-group', to: 'role', title: 'app.basis.permission.role.title' },
      { icon: 'fa-user-large', to: 'user', title: 'app.basis.permission.user.title' }
    ]
  },
  { icon: 'ActivityIcon', to: 'action-log', title: 'app.basis.action-log.title' },
  { icon: 'fa-table-list', to: 'attribute', title: 'app.basis.attribute.title' },
  { icon: 'fa-images', to: 'album', title: 'app.basis.album.title' },
  { icon: 'fa-sitemap', to: 'category', title: 'app.basis.category.title' },
  { icon: 'fa-tag', to: 'tag', title: 'app.basis.tag.title' },
];

export default nodes;
