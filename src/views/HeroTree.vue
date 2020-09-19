<template>
    <div>
        <fast-button appearance="primary" @click="clear">Clear</fast-button>
        <fast-button appearance="primary" @click="random">Random</fast-button>
    </div>
    <!-- <div> -->
    <div style="flex-direction: row; display: flex">
        <tree class="heros" :data="heros" @click="assign"></tree>
        <div v-if="selectedHero">
            <EditHero :hero="selectedHero" :heros="heroList" @save="save(selectedHero)" @cancel="selectedHero = null" />
        </div>
    </div>
</template>

<style scoped>
.heros {
    width: 100%;
    display: flex;
    flex-direction: column;
    flex-wrap: wrap;
}
</style>

<script lang="ts">
import { defineComponent, ref, onUnmounted, reactive, watch } from 'vue';
import {
    bind,
    asObservableCache,
    IChangeSet,
    CompositeDisposable,
    transform,
    distinctValues,
    filterDynamic,
    sort,
    defaultComparer,
    bindSort,
    SortComparer,
    observePropertyChanges,
    autoRefresh,
    transformToTree,
    Node,
} from 'dynamicdatajs';
import { AsyncSubject, Observable, Subject, of } from 'rxjs';
import { publish, refCount } from 'rxjs/operators';
import { Hero as HeroModel } from '../entities';
import { HubConnectionBuilder } from '@microsoft/signalr';
import Hero from '@/components/Hero.vue'; // @ is an alias to /src
import EditHero from '@/components/EditHero.vue';
import Tree from '@/components/Tree.vue';
import { Global } from '@microsoft/fast-element';

export default defineComponent({
    name: 'Heros',
    async setup() {
        const connection = new HubConnectionBuilder().withUrl(process.env.VUE_APP_API_URL ?? '/api').build();

        const cd = new CompositeDisposable();
        const heros = ref<Node<HeroModel, string | undefined>[]>([]);
        const heroList = ref<HeroModel[]>([]);

        const started = new AsyncSubject<unknown>();

        const heroStream = new Observable<IChangeSet<HeroModel, string>>(observer => {
            const handler = (hero: IChangeSet<HeroModel, string>) => {
                observer.next(hero);
                if (started.isStopped) return;
                started.next(void 0);
                started.complete();
            };
            connection.on('herosStream', handler);

            return () => {
                connection.off('herosStream', handler);
            };
        });

        const cache = asObservableCache(heroStream);
        const stream = cache.connect().pipe(transform(observePropertyChanges), autoRefresh(), publish(), refCount());
        cd.add(
            stream
                .pipe(
                    sort(SortComparer.ascending<HeroModel>('name')),
                    bind(heroList.value),
                    // toSortedCollection(z => z.name, 'asc'),

                    sort(SortComparer.ascending<HeroModel>('leaderId').thenByAscending('name')),
                    transformToTree(z => z.leaderId),
                    bind(heros.value),
                )
                .subscribe(),
        );

        onUnmounted(() => {
            connection.stop();
            cd.dispose();
        });

        await Promise.all([connection.start(), started.toPromise()]);

        // for (const team of alignments.value) {
        //     selectedAlignments[team] = true;
        // }
        // for (const team of genders.value) {
        //     selectedGenders[team] = true;
        // }
        // for (const team of races.value) {
        //     selectedRaces[team] = true;
        // }
        // for (const team of publishers.value) {
        //     selectedPublishers[team] = true;
        // }

        const selectedHero = ref<HeroModel | null>(null);

        return {
            connection,
            heros,
            heroList,
            selectedHero,
            async random() {
                await fetch(`${process.env.VUE_APP_API_URL ?? '/api'}/RandomHeros?count=4`, { method: 'POST' });
            },
            async clear() {
                await fetch(`${process.env.VUE_APP_API_URL ?? '/api'}/ClearHeros`, { method: 'POST' });
            },
            async save(hero: HeroModel) {
                await fetch(`${process.env.VUE_APP_API_URL ?? '/api'}/UpdateHero`, { method: 'POST', body: JSON.stringify(hero) });
                selectedHero.value = null;
            },
            assign(hero: Event) {
                let target = hero.target as any;
                do {
                    const item = target?.__vueParentComponent?.props?.node?.item;
                    if (item?.name) {
                        selectedHero.value = item;
                        return;
                    }
                    target = target.parentElement;
                } while (target.parentElement);
            },
        };
    },
    components: {
        // Hero,
        EditHero,
        Tree,
    },
});
</script>
