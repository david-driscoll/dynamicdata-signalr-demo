<template>
    <fast-card>
        <img :src="hero.avatarUrl" />
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
    },
    setup(props) {
        return {};
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