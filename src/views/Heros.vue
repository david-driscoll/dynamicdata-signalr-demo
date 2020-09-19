<template>
    <div>
        <div>
            <fast-button appearance="primary" @click="clear">Clear</fast-button>
            <fast-button appearance="primary" @click="random">Random</fast-button>
            <fieldset style="display: inline-block">
                <legend>Genders</legend>
                <template v-for="item in genders" :key="item">
                    <fast-checkbox :value="item" :checked="selectedGenders[item] || selectedGenders[item] === undefined" @change="selectedGenders[item] = $event.target.checked">{{
                        item
                    }}</fast-checkbox>
                </template>
            </fieldset>
            <fieldset style="display: inline-block">
                <legend>Alignments</legend>
                <template v-for="item in alignments" :key="item">
                    <fast-checkbox
                        :value="item"
                        :checked="selectedAlignments[item] || selectedAlignments[item] === undefined"
                        @change="selectedAlignments[item] = $event.target.checked"
                        >{{ item }}</fast-checkbox
                    >
                </template>
            </fieldset>
        </div>
        <div>
            <fieldset>
                <legend>Races</legend>
                <template v-for="item in races" :key="item">
                    <fast-checkbox :value="item" :checked="selectedRaces[item] || selectedRaces[item] === undefined" @change="selectedRaces[item] = $event.target.checked">{{
                        item
                    }}</fast-checkbox>
                </template>
            </fieldset>
        </div>
        <div>
            <fieldset>
                <legend>Publishers</legend>
                <template v-for="item in publishers" :key="item">
                    <fast-checkbox
                        :value="item"
                        :checked="selectedPublishers[item] || selectedPublishers[item] === undefined"
                        @change="selectedPublishers[item] = $event.target.checked"
                        >{{ item }}</fast-checkbox
                    >
                </template>
            </fieldset>
        </div>
    </div>
    <div style="flex-direction: row; display: flex">
        <div class="heros">
            <transition-group name="hero">
                <Hero v-for="hero in heros" :key="hero.name" :hero="hero" class="hero" @click="selectedHero = hero" />
            </transition-group>
        </div>
        <div v-if="selectedHero">
            <EditHero :hero="selectedHero" :heros="heros" @save="save(selectedHero)" @cancel="selectedHero = null" />
        </div>
    </div>
</template>

<style scoped>
.heros {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
}
.hero {
    margin: 12px 12px;
}
.hero-item {
    display: inline-block;
    margin-right: 10px;
}
.hero-enter-active,
.hero-leave-active {
    transition: all 1s ease;
}
.hero-enter-from,
.hero-leave-to {
    opacity: 0;
    transform: translateY(30px);
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
} from 'dynamicdatajs';
import { AsyncSubject, Observable, Subject, of } from 'rxjs';
import { publish, refCount } from 'rxjs/operators';
import { Hero as HeroModel } from '../entities';
import { HubConnectionBuilder } from '@microsoft/signalr';
import Hero from '@/components/Hero.vue'; // @ is an alias to /src
import EditHero from '@/components/EditHero.vue';

export default defineComponent({
    name: 'Heros',
    async setup() {
        const connection = new HubConnectionBuilder().withUrl(process.env.VUE_APP_API_URL ?? '/api').build();

        const cd = new CompositeDisposable();

        const selectedAlignments = reactive<{ [key: string]: boolean }>({});
        const selectedPublishers = reactive<{ [key: string]: boolean }>({});
        const selectedRaces = reactive<{ [key: string]: boolean }>({});
        const selectedGenders = reactive<{ [key: string]: boolean }>({});
        const refilter = new Subject<unknown>();

        const heros = ref<HeroModel[]>([]);
        const alignments = ref<string[]>([]);
        const races = ref<string[]>([]);
        const genders = ref<string[]>([]);
        const publishers = ref<string[]>([]);

        cd.add(
            watch(
                () => selectedAlignments,
                value => refilter.next(void 0),
                { deep: true },
            ),
            watch(
                () => selectedRaces,
                value => refilter.next(void 0),
                { deep: true },
            ),
            watch(
                () => selectedGenders,
                value => refilter.next(void 0),
                { deep: true },
            ),
            watch(
                () => selectedPublishers,
                value => refilter.next(void 0),
                { deep: true },
            ),
        );

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
                    filterDynamic(
                        of(hero => {
                            return (
                                (selectedAlignments[hero.alignment] === undefined || selectedAlignments[hero.alignment]) &&
                                (selectedGenders[hero.gender] === undefined || selectedGenders[hero.gender]) &&
                                (selectedPublishers[hero.publisher] === undefined || selectedPublishers[hero.publisher]) &&
                                (selectedRaces[hero.race] === undefined || selectedRaces[hero.race])
                            );
                        }),
                        refilter,
                    ),
                    sort(SortComparer.ascending('name'), undefined, refilter),
                    // toSortedCollection(z => z.name, 'asc'),
                    bind(heros.value),
                )
                .subscribe(),
        );
        cd.add(
            stream
                .pipe(
                    transform(z => z.alignment),
                    distinctValues(z => z),
                    sort(defaultComparer),
                    bind(alignments.value),
                )
                .subscribe(),
        );
        cd.add(
            stream
                .pipe(
                    transform(z => z.gender),
                    distinctValues(z => z),
                    sort(defaultComparer),
                    bind(genders.value),
                )
                .subscribe(),
        );
        cd.add(
            stream
                .pipe(
                    transform(z => z.publisher),
                    distinctValues(z => z),
                    sort(defaultComparer),
                    bind(publishers.value),
                )
                .subscribe(),
        );
        cd.add(
            stream
                .pipe(
                    transform(z => z.race),
                    distinctValues(z => z),
                    sort(defaultComparer),
                    bind(races.value),
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
            alignments,
            selectedAlignments,
            races,
            selectedRaces,
            genders,
            selectedGenders,
            publishers,
            selectedPublishers,
            selectedHero,
            async random() {
                await fetch(`${process.env.VUE_APP_API_URL ?? '/api'}/RandomHeros?count=12`, { method: 'POST' });
            },
            async clear() {
                await fetch(`${process.env.VUE_APP_API_URL ?? '/api'}/ClearHeros`, { method: 'POST' });
            },
            async save(hero: HeroModel) {
                await fetch(`${process.env.VUE_APP_API_URL ?? '/api'}/UpdateHero`, { method: 'POST', body: JSON.stringify(hero) });
                selectedHero.value = null;
            },
        };
    },
    components: {
        Hero,
        EditHero,
    },
});
</script>
