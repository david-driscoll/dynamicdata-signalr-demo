<template>
    <fast-tree-view @select="$emit('click', $event.target)">
        <node-tree v-for="node in data" :key="node.key" :node="node" />
    </fast-tree-view>
</template>

<script lang="ts">
import { Hero } from '@/entities';
import { computed, defineComponent, Prop, PropType } from 'vue';
import NodeTree from './NodeTree.vue';

export default defineComponent({
    name: 'Tree',
    props: {
        data: Object,
    },
    components: {
        NodeTree,
    },
});
</script>

<style scoped></style>
