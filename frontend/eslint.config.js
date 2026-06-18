import js from '@eslint/js';
import pluginVue from 'eslint-plugin-vue';
import globals from 'globals';

// ESLint 9 flat config. Uses Vue's "essential" ruleset (matching the prior
// vue3-essential setup) so existing JS single-file components lint cleanly;
// type-aware/TS rules are intentionally not enabled (SFCs are still JS — TS is
// incremental, see tsconfig.json).
export default [
  { ignores: ['dist/**', 'node_modules/**'] },
  js.configs.recommended,
  ...pluginVue.configs['flat/essential'],
  {
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: {
        ...globals.browser,
        ...globals.node,
      },
    },
    rules: {
      'vue/multi-word-component-names': 'off',
    },
  },
];
