<template>
  <DxDataGrid
    :data-source="dataSource"
    :show-borders="true"
  >
    <DxColumn data-field="Name" caption="異動欄位" />
    <DxColumn data-field="Original" caption="異動前" />
    <DxColumn data-field="Current" caption="異動後" />
  </DxDataGrid>
</template>
  
<script>
  import { ref } from '@cloudfun/core';
  import { DxDataGrid, DxColumn } from 'devextreme-vue/data-grid';
  
  export default {
    components: { DxDataGrid, DxColumn },
    props: {
      id: Number,
    },
    data() {
      return {
        dataSource: ref([]),
      };
    },
    async beforeMount() {
      var response = await this.$model.dispatch('actionLog/find', this.id);
      this.dataSource = response.Xml.DeltaColumns;
    }
  };
  </script>
  