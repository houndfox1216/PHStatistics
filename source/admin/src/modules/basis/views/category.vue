<template>
  <div class="grid grid-cols-12 gap-6">
    <div class="col-span-12 lg:col-span-2 2xl:col-span-1 sm:hidden flex mt-6">
      <h2 class="intro-y text-lg font-medium mr-auto pl-2">{{($breadcrumb[$breadcrumb.length-1] as ISitemapNode).title}}</h2>
    </div>
    <!-- BEGIN: Content -->
    <div class="intro-y col-span-12 box mt-0 sm:mt-6 p-3">
      <DxTreeList
        ref="categoryTree"
        :data-source="categoryStore"
        :root-value="-1"
        :show-row-lines="true"
        :show-borders="true"
        :column-auto-width="true"
        key-expr="Id"
        parent-id-expr="ParentId"
        @init-new-row="onTreeAdding"
        @editing-start="onTreeEditing"
        @toolbarPreparing="(e: any) => e.toolbarOptions.items.unshift({ location: 'after', widget: 'dxButton', options: { 
          icon: 'refresh', 
          hint: '重置狀態', 
          onClick: function() { 
            e.component.state(null);
            e.component.refresh();
          }
        }})"
      >
        <DxStateStoring :enabled="true" type="localStorage" storage-key="category-tree" />
        <DxFilterRow
          :visible="true"
        />
        <DxColumnChooser :enabled="true" />
        <DxSearchPanel :visible="true" />
        <DxHeaderFilter :visible="true"/>
        <DxScrolling mode="standard" />
        <DxPaging :enabled="true" :page-size="10" />  
        <DxPager :show-page-size-selector="true" :allowed-page-sizes="[10, 20, 50]" :show-navigation-buttons="true" :show-info="true" />
        <DxRowDragging
          :on-reorder="onTreeReorder"
          :allow-drop-inside-item="true"
          :allow-reordering="true"
          :show-drag-icons="true"
        />
        <DxColumn data-field="Name" caption="名稱" :allow-sorting="false" />
        <DxColumn data-field="Ordinal" caption="排序值" data-type="number" :width="100" sort-order="asc" :allow-sorting="false" />
        <DxColumn data-field="Published" caption="已發佈" data-type="boolean" editor-type="dxSwitch" :width="100" :allow-sorting="false" />
        <DxColumn data-field="ParentId" caption="父類別" :allow-hiding="false" :visible="false" />
        <DxEditing :allow-adding="true" :allow-updating="true" :allow-deleting="true" :use-icons="true" />
      </DxTreeList>
    </div>
    <!-- END: Content -->
  </div>
</template>

<script lang="ts">
import { defineComponent, ISitemapNode, ref } from '@cloudfun/core'

import CustomStore from 'devextreme/data/custom_store';
import { EditingStartEvent } from 'devextreme/ui/tree_list';
import { DxSwitch } from 'devextreme-vue/switch';
import { 
  DxTreeList, 
  DxColumn, 
  DxRowDragging, 
  DxFilterRow, 
  DxStateStoring, 
  DxColumnChooser, 
  DxSearchPanel,
  DxHeaderFilter,
  DxScrolling,
  DxPaging,
  DxPager,
  DxEditing,
} from 'devextreme-vue/tree-list';

export default defineComponent({
  components: {
    DxTreeList,
    DxColumn,
    DxRowDragging,
    DxFilterRow,
    DxStateStoring,
    DxColumnChooser,
    DxSearchPanel,
    DxHeaderFilter,
    DxScrolling,
    DxPaging,
    DxPager,
    DxEditing,
    DxSwitch,
  },
  setup () {
    return {
      categoryTree: ref<any>({}),
      categoryStore: ref<CustomStore>(),
      editingRow: ref<any>({}),
    };
  },
  async beforeMount() {
    this.categoryStore = await this.$model.dispatch('category/getStore');
  },
  methods: {
    onTreeAdding(e: any) {
      if (e.data.ParentId === -1) delete e.data.ParentId;
      e.data.Published = 0;
      this.editingRow = e.data;
    },
    onTreeEditing(e: EditingStartEvent) {
      this.editingRow = e.data;
    },
    onTreeReorder(e: any) {
      if (e.toIndex === -1) e.cancel = true;
      else {
        const source = e.itemData;
        const visibleRows = e.component.getVisibleRows();
        const targetIndex = e.dropInsideItem || e.fromIndex > e.toIndex ? e.toIndex : e.toIndex + 1;
        const target = visibleRows[targetIndex < visibleRows.length ? targetIndex : visibleRows.length-1].data;
        const isAfter = !e.dropInsideItem && targetIndex >= visibleRows.length;
        if (e.dropInsideItem) {
          source.ParentId = target.Id;
          this.$model.dispatch('category/update', source).then(
            () => e.component.refresh(),
            failure => {
              this.$send('error', { 
                subject: this.$tc("model.error.update"), 
                content: failure.message 
              });
              e.cancel = true;
            }
          );
        } else {
          this.$model.dispatch('category/reorder', { source: source.Id, target: target.Id, isAfter }).then(
            () => e.component.refresh(),
            failure => {
              this.$send('error', { 
                subject: this.$tc("model.error.update"), 
                content: failure.message 
              });
              e.cancel = true;
            }
          );
        }
      }
    }
  }
})
</script>
