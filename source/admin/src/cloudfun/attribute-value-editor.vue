<template>
  <DxDataGrid
    ref="attributeValueGrid"
    max-height="400"
    :show-borders="true"
    :data-source="groupValues"
    :allow-column-resizing="true"
    :row-alternation-enabled="true"
    :repaint-changes-only="true"
    @row-prepared="(e: RowPreparedEvent) => {
      if (e.rowType === 'data' && e.data.Attribute.Required) e.rowElement.style.backgroundColor = '#F000003F';
    }"
  >
    <DxColumn data-field="Attribute.Name" caption="名稱" :allow-editing="false" :width="150" />
    <DxColumn data-field="Attribute.Code" caption="代碼" :allow-editing="false" :width="150" />
    <DxColumn data-field="InputValue.TextValue" :visible="false" />
    <DxColumn data-field="InputValue.DecimalValue" :visible="false" />
    <DxColumn data-field="InputValue.Value" :visible="false" />
    <DxColumn data-field="SelectedValues" caption="值" cell-template="attribute-value-template" />
    <template #attribute-value-template="{ data: cell }">
      <div v-if="!cell.data.Attribute.Selectable">
        <div class="flex">
          <DxTextBox 
            class="w-1/3 mr-1" 
            placeholder="請輸入顯示用文字" 
            :value="cell.data.InputValue.TextValue" 
            @value-changed="(e: any) => onChange('InputValue.TextValue', cell, e.value)" 
          />
          <DxNumberBox 
            class="w-1/3 mr-1" 
            placeholder="請輸入比較用數值" 
            :value="cell.data.InputValue.DecimalValue"
            @value-changed="(e: any) => onChange('InputValue.DecimalValue', cell, e.value)" 
          />
          <DxTextBox 
            class="w-1/3" 
            placeholder="請輸入實際值" 
            :value="cell.data.InputValue.Value" 
            @value-changed="(e: any) => onChange('InputValue.Value', cell, e.value)" 
          />
        </div>
      </div>
      <DxTagBox v-else-if="cell.data.Attribute.Multiple"
        placeholder="請選擇" 
        :data-source="cell.data.Attribute.Values"
        display-expr="TextValue"
        value-expr="Id"
        :value="cell.data.SelectedValues" 
        @value-changed="(e: any) => onChange('SelectedValues', cell, e.value)" 
      />
      <DxSelectBox v-else
        placeholder="請選擇" 
        :data-source="cell.data.Attribute.Values"
        display-expr="TextValue"
        value-expr="Id"
        :value="cell.data.SelectedValues" 
        @value-changed="(e: any) => onChange('SelectedValues', cell, e.value)" 
      />
    </template>
  </DxDataGrid>
</template>

<script lang="ts">
import { defineComponent, PropType, ref } from "@cloudfun/core";

import { DxTextBox, DxNumberBox, DxSelectBox, DxTagBox } from 'devextreme-vue';
import {
  DxDataGrid,
  DxEditing,
  DxColumn,
} from 'devextreme-vue/data-grid';
import { RowPreparedEvent } from "devextreme/ui/data_grid";

export default defineComponent({
  props: {
    attributes: { type: Array as PropType<any[]>, required: true },
    value: Array as PropType<{ AttributeValueId: number, AttributeValue: { AttributeId: number, Attribute: any, TextValue: string, DecimalValue: number, Value: string }}[]>,
    modelValue: Array as PropType<{ AttributeValueId: number, AttributeValue: { AttributeId: number, Attribute: any, TextValue: string, DecimalValue: number, Value: string }}[]>,
  },
  components: {
    DxDataGrid,
    DxEditing,
    DxColumn,
    DxTextBox,
    DxNumberBox,
    DxSelectBox,
    DxTagBox,
},
  setup() {
    return {
      attributeValueGrid: ref<any>({}),
      groupValues: ref<any>([]),
      values: ref<any>([]),
    };
  },
  watch: {
    value(current: any) {
      if (current !== this.values) this.computeValues(current);
    },
    modelValue(current: any) {
      if (current !== this.values) this.computeValues(current);
    }
  },
  mounted() {
    // Generate containers for each attributes
    for (let attribute of this.attributes) {
      this.groupValues.push({
        Attribute: {
          Id: attribute.Id,
          Name: attribute.Name,
          Code: attribute.Code,
          Ordinal: attribute.Ordinal,
          Required: attribute.Required,
          Selectable: attribute.Selectable,
          Values: attribute.Values,
          Multiple: attribute.Multiple
        },
        InputValue: { Id: undefined, TextValue: undefined, DecimalValue: undefined, Value: undefined },
        SelectedValues: attribute.Multiple ? [] : undefined,
      });
    }
    const value = this.modelValue ?? this.value;
    this.computeValues(value);
  },
  methods: {
    computeValues(value?: { AttributeValueId: number, AttributeValue: { AttributeId: number, Attribute: any, TextValue: string, DecimalValue: number, Value: string }}[]) {
      if (value) {
        // Push value into the container of the assigned attribute
        if (Array.isArray(value)) {
          for (const item of value) {
            if (item.AttributeValue) {
              delete item.AttributeValue.Attribute;
              const group = this.groupValues.find((e: any) => e.Attribute.Id === item.AttributeValue.AttributeId);
              if (group) {
                if (group.Attribute.Selectable) {
                  if (group.Attribute.Multiple) group.SelectedValues.push(item.AttributeValueId);
                  else group.SelectedValues = item.AttributeValueId;
                } else {
                  group.InputValue.Id = item.AttributeValueId;
                  group.InputValue.TextValue = item.AttributeValue.TextValue;
                  group.InputValue.DecimalValue = item.AttributeValue.DecimalValue;
                  group.InputValue.Value = item.AttributeValue.Value;
                }
              }
            }
          }
        }
      }
      // Make the default value when the attribute is required.
      for (const group of this.groupValues) {
        if (group.Attribute.Required) {
          if (group.Attribute.Selectable) {
            if (group.Multiple && !group.SelectedValues.length) group.SelectedValues.push(group.Attribute.Values[0].Id);
            else if (!group.SelectedValues) group.SelectedValues = group.Attribute.Values[0].Id;
          } 
        }
      }
      // Generate values
      this.values = [];
      for (const group of this.groupValues) {
        if (group.Attribute.Selectable) {
          if (Array.isArray(group.SelectedValues)) {
            for (const id of group.SelectedValues) {
              this.values.push({
                AttributeValueId: id,
                AttributeValue: { AttributeId: group.Attribute.Id, Id: id },
              });
            }
          } else {
            this.values.push({
              AttributeValueId: group.SelectedValues,
              AttributeValue: { AttributeId: group.Attribute.Id, Id: group.SelectedValues },
            });
          }
        } else {
          this.values.push({
            AttributeValueId: group.InputValue.Id,
            AttributeValue: { 
              Id: group.InputValue.Id,
              AttributeId: group.Attribute.Id,
              TextValue: group.InputValue.TextValue,
              DecimalValue: group.InputValue.DecimalValue,
              Value: group.InputValue.Value,
             },
          });
        }
      }
    },
    onChange(field: string, cell: any, value: any) {
      // Make attribute value and assign it
      this.attributeValueGrid.instance.cellValue(cell.rowIndex, field, value);
      const group = this.groupValues.find((e: any) => e.Attribute.Id === cell.data.Attribute.Id);
      if (field.startsWith("InputValue.")) group.InputValue[field.substring(11)] = value;
      else group.SelectedValues = value;
      this.computeValues();
      this.$emit("change", this.values);
      this.$emit("update:modelValue", this.values);
    }
  }
});
</script>