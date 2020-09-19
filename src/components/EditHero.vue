<template>
    <fast-card>
        <div>
            <img :src="hero.avatarUrl" />
            <fieldset>
                <legend>Id</legend>
                <input type="text" v-model="heroValue.id" />
            </fieldset>
            <fieldset>
                <legend>Leader</legend>
                <select v-model="heroValue.leaderId">
                    <option :value="-1">None</option>
                    <option v-for="item in heros" :key="item.Id" :value="item.id">{{item.name}}</option>
                </select>
            </fieldset>
            <fieldset>
                <legend>Name</legend>
                <input type="text" v-model="heroValue.name" />
            </fieldset>
            <fieldset>
                <legend>Real Name</legend>
                <input type="text" v-model="heroValue.realName" />
            </fieldset>
            <fieldset>
                <legend>Alignment</legend>
                <input type="text" v-model="heroValue.alignment" />
            </fieldset>
            <fieldset>
                <legend>Gender</legend>
                <input type="text" v-model="heroValue.gender" />
            </fieldset>
            <fieldset>
                <legend>Race</legend>
                <input type="text" v-model="heroValue.race" />
            </fieldset>
            <fieldset>
                <legend>Publisher</legend>
                <input type="text" v-model="heroValue.publisher" />
            </fieldset>
            <button type="button" @click="save">Save</button>
            <button type="button" @click="cancel">Cancel</button>
        </div>
    </fast-card>
</template>

<script lang="ts">
import { Hero } from '@/entities';
import { computed, defineComponent, PropType } from 'vue';

export default defineComponent({
    name: 'EditHero',
    emits: ['save', 'cancel'],
    props: {
        hero: {
            type: Object as PropType<Hero>,
            required: true,
        },
        heros: {
            type: Object as PropType<Hero[]>,
            required: true,
        },
    },
    methods: {
        save() {
            this.$emit('save', this.heroValue);
        },
        cancel() {
            this.$emit('cancel', this.heroValue);
        },
    },
    setup(props) {
        return {
            heroValue: computed(() => props.hero),
        };
    },
});
</script>

<style scoped>
img {
    max-height: 160px;
    width: 160px;
}
</style>