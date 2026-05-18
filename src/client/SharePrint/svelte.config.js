import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	compilerOptions: {
		// Force runes mode for the project, except for libraries. Can be removed in svelte 6.
		runes: ({ filename }) => (filename.split(/[/\\]/).includes('node_modules') ? undefined : true)
	},
	kit: {
		// SPA mode: static adapter with SPA fallback. Backend is the separate SharePrint.Api.
		adapter: adapter({ fallback: 'index.html' })
	}
};

export default config;
