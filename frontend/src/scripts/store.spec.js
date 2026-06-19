import { describe, it, expect, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useAuthStore } from '@/scripts/store';

describe('auth store (Pinia)', () => {
  beforeEach(() => setActivePinia(createPinia()));

  it('starts with no token', () => {
    expect(useAuthStore().getJwtToken).toBeNull();
  });

  it('setJwtToken updates the token', () => {
    const store = useAuthStore();
    store.setJwtToken('jwt-abc');
    expect(store.getJwtToken).toBe('jwt-abc');
  });
});
