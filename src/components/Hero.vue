<template>
    <fast-card>
        <img :src="hero.avatarUrl" />
        <div style="padding: 0 10px 10px; color: var(--neutral-foreground-rest)">
            <h2>{{hero.name}}</h2>
            <p></p>
            <fast-button>Button</fast-button>
        </div>
    </fast-card>
</template>

<script lang="ts">
import { defineComponent } from 'vue';

export default defineComponent({
    name: 'HelloWorld',
    props: {
        hero: Object,
    },
});
</script>

<style scoped>
fast-card {
    /* display: flex; */
    width: 240px;
}
img {
    max-height: 240px;
    width: 240px;
}
</style>