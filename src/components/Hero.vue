<template>
    <fast-card>
        <img :src="hero.avatarUrl" @click="select()" />
        <div style="padding: 0 10px 10px; color: var(--neutral-foreground-rest)">
            <h4>{{hero.name}}</h4>
            <p>{{hero.alignment}} | {{hero.gender}} | {{hero.race}}</p>
            <!-- <p></p> -->
            <!--
            <ul v-for="team in hero.teams" :key="team">
                <li>{{team}}</li>
            </ul>
            -->
            <!-- <fast-button>Button</fast-button> -->
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
        click: {
            type: Function as PropType<() => void>,
        },
    },
    setup(props) {
        return {
            select() {
                return props.click?.();
            },
        };
    },
});
</script>

<style scoped>
fast-card {
    /* display: flex; */
    width: 160px;
    max-height: 260px;
}
img {
    max-height: 160px;
    width: 160px;
}
</style>