<template>
    <fast-tree-item expanded>
        <span @click="$emit('click', node.item)">
            <img :src="node.item.avatarUrl" height="24" width="24" />
            {{ node.item.name }}
        </span>
        <Node v-for="child in children" :node="child" :key="child.key" v-on="on" />
    </fast-tree-item>
</template>

<script lang="ts">
import { defineComponent, onUnmounted, PropType, ref } from 'vue';
import { Node, bind, CompositeDisposable } from 'dynamicdatajs';
import { Hero as HeroModel } from '../entities';
export default defineComponent({
    name: 'Node',
    emits: ['click'],
    props: {
        node: {
            type: Object as PropType<Node<unknown, unknown | undefined>>,
            required: true,
        },
    },
    setup(props) {
        const cd = new CompositeDisposable();
        const children = ref<Node<unknown, unknown | undefined>[]>([]);
        cd.add(props.node.children.connect().pipe(bind(children.value)).subscribe());

        onUnmounted(() => {
            cd.dispose();
        });
        return {
            children,
        };
    },
});
</script>
