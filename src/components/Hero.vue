<template>
    <fast-card>
        <img :src="hero.avatarUrl" />
        <div style="padding: 0 10px 10px; color: var(--neutral-foreground-rest)">
            <h2>{{hero.name}}</h2>
            <p></p>
            <!--
            <ul v-for="team in hero.teams" :key="team">
                <li>{{team}}</li>
            </ul>
            -->
            <fast-button>Button</fast-button>
        </div>
    </fast-card>
</template>

<script lang="ts">
import { Hero } from '@/entities';
import { computed, defineComponent, Prop, PropType } from 'vue';

export default defineComponent({
    name: 'Hero',
    props: {
        hero: {
            type: Object as PropType<Hero>,
            required: true,
        },
    },
    setup(props) {
        return {};
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