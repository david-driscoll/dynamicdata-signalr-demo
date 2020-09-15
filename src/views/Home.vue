<template>
    <div class="home">
        <img alt="Vue logo" src="../assets/logo.png" />

        <div v-for="hero in heroes" :key="hero.id">{{hero.name}}</div>
    </div>
</template>

<script lang="ts">
import { defineComponent, ref, onBeforeMount, onUnmounted } from 'vue';
import { bind, Change, SourceCache, NotifyChanged, asObservableCache, IChangeSet } from 'dynamicdatajs';
import { Observable } from 'rxjs';
import { Hero } from '../entities';
import { HubConnectionBuilder } from '@microsoft/signalr';
import HelloWorld from '@/components/HelloWorld.vue'; // @ is an alias to /src

export default defineComponent({
    name: 'Home',
    setup() {
        const connection = new HubConnectionBuilder().withUrl('/api').build();

        const heroes = ref<Hero[]>([]);

        const heroStream = new Observable<IChangeSet<Hero, string>>(observer => {
            const handler = (hero: IChangeSet<Hero, string>) => {
                observer.next(hero);
                console.log(hero);
            };
            connection.on('heroesStream', handler);

            return () => {
                connection.off('heroesStream', handler);
            };
        });

        const sub = asObservableCache(heroStream).connect().pipe(bind(heroes.value)).subscribe();

        onBeforeMount(() => connection.start());
        onUnmounted(() => {
            connection.stop();
            sub.unsubscribe();
        });
        // connection

        return {
            connection,
            heroes: heroes,
        };
    },
    components: {
        // HelloWorld,
    },
});
</script>
