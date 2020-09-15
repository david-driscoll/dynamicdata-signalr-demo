<template>
    <div class="heroes">
        <Hero v-for="hero in heroes" :key="hero.id" :hero="hero" class="hero" />
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
</style>

<script lang="ts">
import { defineComponent, ref, onBeforeMount, onUnmounted } from 'vue';
import { bind, Change, SourceCache, NotifyChanged, asObservableCache, IChangeSet } from 'dynamicdatajs';
import { Observable } from 'rxjs';
import { Hero as HeroModel } from '../entities';
import { HubConnectionBuilder } from '@microsoft/signalr';
import Hero from '@/components/Hero.vue'; // @ is an alias to /src

export default defineComponent({
    name: 'Home',
    setup() {
        const connection = new HubConnectionBuilder().withUrl('/api').build();

        const heroes = ref<HeroModel[]>([]);

        const heroStream = new Observable<IChangeSet<HeroModel, string>>(observer => {
            const handler = (hero: IChangeSet<HeroModel, string>) => {
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
        Hero,
    },
});
</script>
