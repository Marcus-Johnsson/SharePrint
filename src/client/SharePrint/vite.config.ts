import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [sveltekit()],
	// Dev proxy: keep /api same-origin so the auth cookie is same-site.
	// Target = SharePrint.Api http profile (Properties/launchSettings.json).
	server: { proxy: { '/api': 'http://localhost:5136' } }
});
