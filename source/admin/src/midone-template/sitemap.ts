import { ISitemapNode } from '@cloudfun/core'

const nodes: (string | ISitemapNode)[] = [
  { icon: 'fa-columns', title: 'Dashboards', subNodes: [
    { icon: "", to: "midone/dashboard-overview-1", title: "Overview 1" },
    { icon: "", to: "midone/dashboard-overview-2", title: "Overview 2" },
    { icon: "", to: "midone/dashboard-overview-3", title: "Overview 3" },
    { icon: "", to: "midone/dashboard-overview-4", title: "Overview 4" },
  ]},
  { icon: 'InboxIcon', to: 'midone/inbox', title: 'Inbox' },
  { icon: 'HardDriveIcon', to: 'midone/file-manager', title: 'File Manager' },
  { icon: 'CreditCardIcon', to: 'midone/point-of-sale', title: 'Point of Sale' },
  { icon: 'MessageSquareIcon', to: 'midone/chat', title: 'Chat' },
  { icon: 'FileTextIcon', to: 'midone/post', title: 'Post' },
  { icon: "CalendarIcon", to: "midone/calendar", title: "Calendar" },
  { icon: 'EditIcon', title: 'Crud', subNodes: [
    { icon: '', to: 'midone/crud-data-list', title: 'Data List' },
    { icon: '', to: 'midone/crud-form', title: 'Form' }
  ]},
  { icon: 'UsersIcon', title: 'Users', subNodes: [
    { icon: '', to: 'midone/users-layout-1', title: 'Layout 1' },
    { icon: '', to: 'midone/users-layout-2', title: 'Layout 2' },
    { icon: '', to: 'midone/users-layout-3', title: 'Layout 3' },
  ]},
  { icon: 'TrelloIcon', title: 'Profiles', subNodes: [
    { icon: '', to: 'midone/profile-overview-1', title: 'Overview 1' },
    { icon: '', to: 'midone/profile-overview-2', title: 'Overview 2' },
    { icon: '', to: 'midone/profile-overview-3', title: 'Overview 3' },
  ]},
  "Pages",
  { icon: 'LayoutIcon', title: 'Wizards', subNodes: [
    { icon: '', to: 'midone/wizard-layout-1', title: 'Layout 1' },
    { icon: '', to: 'midone/wizard-layout-2', title: 'Layout 2' },
    { icon: '', to: 'midone/wizard-layout-3', title: 'Layout 3' },
  ]},
  { icon: 'LayoutIcon', title: 'Blog', subNodes: [
    { icon: '', to: 'midone/blog-layout-1', title: 'Layout 1' },
    { icon: '', to: 'midone/blog-layout-2', title: 'Layout 2' },
    { icon: '', to: 'midone/blog-layout-3', title: 'Layout 3' },
  ]},
  { icon: 'LayoutIcon', title: 'Pricing', subNodes: [
    { icon: '', to: 'midone/pricing-layout-1', title: 'Layout 1' },
    { icon: '', to: 'midone/pricing-layout-2', title: 'Layout 2' },
  ]},
  { icon: 'LayoutIcon', title: 'Invoice', subNodes: [
    { icon: '', to: 'midone/invoice-layout-1', title: 'Layout 1' },
    { icon: '', to: 'midone/invoice-layout-2', title: 'Layout 2' },
  ]},
  { icon: 'LayoutIcon', title: 'FAQ', subNodes: [
    { icon: '', to: 'midone/faq-layout-1', title: 'Layout 1' },
    { icon: '', to: 'midone/faq-layout-2', title: 'Layout 2' },
    { icon: '', to: 'midone/faq-layout-3', title: 'Layout 3' },
  ]},
  { icon: 'LayoutIcon',  to: '/login', title: 'Login' },
  { icon: 'LayoutIcon',  to: '/register', title: 'Register' },
  { icon: 'LayoutIcon',  to: '/error-page', title: 'Error Page' },
  { icon: 'LayoutIcon',  to: 'midone/update-profile', title: 'Update profile' },
  { icon: 'LayoutIcon',  to: 'midone/change-password', title: 'Change Password' },
  "Components",
  { icon: 'InboxIcon', title: 'Table', subNodes: [
    { icon: '', to: 'midone/regular-table', title: 'Regular Table' },
  ]},
  { icon: "InboxIcon", title: "Overlay", subNodes: [
    { icon: "", to: "midone/modal", title: "Modal" },
    { icon: "", to: "midone/slide-over", title: "Slide Over" },
    { icon: "", to: "midone/notification", title: "Notification" },
  ]},
  { icon: "InboxIcon", to: "midone/tab", title: "Tab" },
  { icon: 'InboxIcon', to: 'midone/accordion', title: 'Accordion' },
  { icon: 'InboxIcon', to: 'midone/button', title: 'Button' },
  { icon: 'InboxIcon', to: 'midone/alert', title: 'Alert' },
  { icon: 'InboxIcon', to: 'midone/progress-bar', title: 'Progress Bar' },
  { icon: 'InboxIcon', to: 'midone/tooltip', title: 'Tooltip' },
  { icon: 'InboxIcon', to: 'midone/dropdown', title: 'Dropdown' },
  { icon: 'InboxIcon', to: 'midone/typography', title: 'Typography' },
  { icon: 'InboxIcon', to: 'midone/icon', title: 'Icon' },
  { icon: 'InboxIcon', to: 'midone/loading-icon', title: 'Loading Icon' },
  { icon: 'SidebarIcon', title: 'Forms', subNodes: [
    { icon: '', to: 'midone/regular-form', title: 'Regular Form' },
    { icon: '', to: 'midone/datepicker', title: 'Datepicker' },
    { icon: '', to: 'midone/tom-select', title: "Tom Select" },
    { icon: '', to: 'midone/file-upload', title: 'File Upload' },
    { icon: '', to: 'midone/wysiwyg-editor', title: 'Wysiwyg Editor' },
    { icon: '', to: 'midone/validation', title: 'Validation' }
  ]},
  { icon: 'HardDriveIcon', title: 'Widgets', subNodes: [
    { icon: '', to: 'midone/chart', title: 'Chart' },
    { icon: '', to: 'midone/slider', title: 'Slider' },
    { icon: '', to: 'midone/image-zoom', title: 'Image Zoom' }
  ]}
];

export default nodes;
