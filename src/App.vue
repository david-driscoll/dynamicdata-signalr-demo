<template>
    <div id="nav">
        <router-link to="/">Home</router-link>|
        <router-link to="/about">About</router-link>
    </div>

    <Suspense>
        <template #default>
            <router-view />
        </template>
        <template #fallback>
            <fast-progress-ring />
        </template>
    </Suspense>
</template>

<script lang="ts">
import { defineComponent, onBeforeMount, onMounted } from 'vue';
import { parseColorHexRGB } from '@microsoft/fast-colors';
import { createColorPalette, FASTDesignSystemProvider } from '@microsoft/fast-components';

const neutralPalette = createColorPalette(parseColorHexRGB('#2882DB'));
const accentPalette = createColorPalette(parseColorHexRGB('#DB8128'));
export default defineComponent({
    setup() {
        onBeforeMount(() => {
            const provider = document.querySelector('fast-design-system-provider') as FASTDesignSystemProvider;
            provider.neutralPalette = neutralPalette;
            provider.accentPalette = accentPalette;
        });
        return {};
    },
});
</script>

<style>
body {
    padding: 0%;
    margin: 0%;
    height: 100%;
    min-height: 100vh;
    display: flex;
    align-items: stretch;
}
#app {
    font-family: Avenir, Helvetica, Arial, sans-serif;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
    text-align: center;
    color: #2c3e50;
    flex: 1;
    align-self: center;
    margin-bottom: auto;
    height: 100vh;
}

#nav {
    padding: 30px;
}

#nav a {
    font-weight: bold;
    color: #2c3e50;
}

#nav a.router-link-exact-active {
    color: #42b983;
}
</style>
