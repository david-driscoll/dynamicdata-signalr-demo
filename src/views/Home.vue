<template>
    <div>
        <div>
            <fieldset style="display: inline-block;">
                <legend>Genders</legend>
                <template v-for="item in genders" :key="item">
                    <fast-checkbox
                        :value="item"
                        :checked="selectedGenders[item] || selectedGenders[item]=== undefined"
                        @change="selectedGenders[item] = $event.target.checked"
                    >{{item}}</fast-checkbox>
                </template>
            </fieldset>
            <fieldset style="display: inline-block;">
                <legend>Alignments</legend>
                <template v-for="item in alignments" :key="item">
                    <fast-checkbox
                        :value="item"
                        :checked="selectedAlignments[item] || selectedAlignments[item]=== undefined"
                        @change="selectedAlignments[item] = $event.target.checked"
                    >{{item}}</fast-checkbox>
                </template>
            </fieldset>
        </div>
        <div>
            <fieldset>
                <legend>Races</legend>
                <template v-for="item in races" :key="item">
                    <fast-checkbox
                        :value="item"
                        :checked="selectedRaces[item] || selectedRaces[item]=== undefined"
                        @change="selectedRaces[item] = $event.target.checked"
                    >{{item}}</fast-checkbox>
                </template>
            </fieldset>
        </div>
        <div>
            <fieldset>
                <legend>Publishers</legend>
                <template v-for="item in publishers" :key="item">
                    <fast-checkbox
                        :value="item"
                        :checked="selectedPublishers[item] || selectedPublishers[item]=== undefined"
                        @change="selectedPublishers[item] = $event.target.checked"
                    >{{item}}</fast-checkbox>
                </template>
            </fieldset>
        </div>
    </div>
    <div class="heroes">
        <transition-group name="hero">
            <Hero v-for="hero in heroes" :key="hero.name" :hero="hero" class="hero" />
        </transition-group>
    </div>
</template>

<style scoped>
.heroes {
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
import { defineComponent, ref, onBeforeMount, onUnmounted, reactive, computed, watch } from 'vue';
import {
    bind,
    Change,
    SourceCache,
    NotifyChanged,
    asObservableCache,
    IChangeSet,
    CompositeDisposable,
    transform,
    distinctValues,
    keyValueComparer,
    filterDynamic,
    sort,
    defaultComparer,
    bindSort,
    toSortedCollection,
    SortComparer,
    transformMany,
} from 'dynamicdatajs';
import { from, toArray } from 'ix/iterable';
import { filter as ixFilter, map as ixMap } from 'ix/iterable/operators';
import { AsyncSubject, Observable, Subject, of } from 'rxjs';
import { distinct, take, tap } from 'rxjs/operators';
import { Hero as HeroModel } from '../entities';
import { HubConnectionBuilder } from '@microsoft/signalr';
import Hero from '@/components/Hero.vue'; // @ is an alias to /src

export default defineComponent({
    name: 'Home',
    async setup() {
        const connection = new HubConnectionBuilder().withUrl(process.env.VUE_APP_API_URL ?? '/api').build();

        const cd = new CompositeDisposable();

        const selectedAlignments = reactive<{ [key: string]: boolean }>({});
        const selectedPublishers = reactive<{ [key: string]: boolean }>({});
        const selectedRaces = reactive<{ [key: string]: boolean }>({});
        const selectedGenders = reactive<{ [key: string]: boolean }>({});
        const refilter = new Subject<unknown>();

        const heroes = ref<HeroModel[]>([]);
        const alignments = ref<string[]>([]);
        const races = ref<string[]>([]);
        const genders = ref<string[]>([]);
        const publishers = ref<string[]>([]);

        const started = new AsyncSubject<unknown>();

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

        const heroStream = new Observable<IChangeSet<HeroModel, string>>(observer => {
            const handler = (hero: IChangeSet<HeroModel, string>) => {
                observer.next(hero);
                if (started.isStopped) return;
                started.next(void 0);
                started.complete();
            };
            connection.on('heroesStream', handler);

            return () => {
                connection.off('heroesStream', handler);
            };
        });

        const cache = asObservableCache(heroStream);
        cd.add(
            cache
                .connect()
                .pipe(
                    transform(z => z.alignment),
                    distinctValues(z => z),
                    sort(defaultComparer),
                    bindSort(alignments.value),
                )
                .subscribe(),
        );
        cd.add(
            cache
                .connect()
                .pipe(
                    transform(z => z.gender),
                    distinctValues(z => z),
                    sort(defaultComparer),
                    bindSort(genders.value),
                )
                .subscribe(),
        );
        cd.add(
            cache
                .connect()
                .pipe(
                    transform(z => z.publisher),
                    distinctValues(z => z),
                    sort(defaultComparer),
                    bindSort(publishers.value),
                )
                .subscribe(),
        );
        cd.add(
            cache
                .connect()
                .pipe(
                    transform(z => z.race),
                    distinctValues(z => z),
                    sort(defaultComparer),
                    bindSort(races.value),
                )
                .subscribe(),
        );
        cd.add(
            cache
                .connect()
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
                    bindSort(heroes.value, bind.deepEqualAdapter(heroes.value)),
                )
                // .subscribe(z => (heroes.value = z))
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

        return {
            connection,
            heroes,
            alignments,
            selectedAlignments,
            races,
            selectedRaces,
            genders,
            selectedGenders,
            publishers,
            selectedPublishers,
        };
    },
    components: {
        Hero,
    },
});
</script>
