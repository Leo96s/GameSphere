import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createApp, h, nextTick } from 'vue';
import { RouterView, createMemoryHistory, createRouter } from 'vue-router';
import QuizCatalogPage from './QuizCatalogPage.vue';
import { listQuizzes } from '@/services/quizService';

vi.mock('@/services/quizService', () => ({ listQuizzes: vi.fn() }));

const quiz = (id, title) => ({ id, title, category: 1, difficulty: 0, numberOfQuests: 5 });

const deferred = () => {
  let resolve;
  const promise = new Promise((r) => { resolve = r; });
  return { promise, resolve };
};

let app;
let host;
let router;

const mountAt = async (url) => {
  router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/quizzes', name: 'quiz-catalog', component: QuizCatalogPage },
      { path: '/quizzes/:id', name: 'quiz-play', component: { render: () => h('p', 'play') } },
      { path: '/outra', name: 'other', component: { render: () => h('p', 'outra') } },
    ],
  });
  router.push(url);
  await router.isReady();
  host = document.createElement('div');
  document.body.appendChild(host);
  app = createApp({ render: () => h(RouterView) });
  app.use(router);
  app.mount(host);
  await nextTick();
};

const select = (label) => {
  const el = [...host.querySelectorAll('label')].find((l) => l.textContent.trim() === label);
  return host.querySelector(`#${el.getAttribute('for')}`);
};

const choose = async (label, value, expectedQuery) => {
  const el = select(label);
  el.value = value;
  el.dispatchEvent(new Event('change', { bubbles: true }));
  await vi.waitFor(() => expect(router.currentRoute.value.query).toEqual(expectedQuery));
  await nextTick();
};

const settle = async () => {
  await new Promise((r) => setTimeout(r, 20));
  await nextTick();
};

beforeEach(() => {
  vi.mocked(listQuizzes).mockReset();
  vi.mocked(listQuizzes).mockResolvedValue([quiz(1, 'Quiz A')]);
});

afterEach(() => {
  app?.unmount();
  host?.remove();
  app = null;
  host = null;
});

describe('QuizCatalogPage', () => {
  it('carrega a lista inicial a partir de ?category=1', async () => {
    await mountAt('/quizzes?category=1');
    await vi.waitFor(() => expect(host.textContent).toContain('Quiz A'));
    expect(listQuizzes).toHaveBeenCalledTimes(1);
    expect(listQuizzes).toHaveBeenCalledWith({ category: 1, difficulty: null });
    expect(select('Categoria').value).toBe('1');
    expect(select('Dificuldade').value).toBe('');
  });

  it('mudar um select actualiza a query e faz novo pedido com os filtros', async () => {
    await mountAt('/quizzes');
    await vi.waitFor(() => expect(host.textContent).toContain('Quiz A'));
    await choose('Dificuldade', '2', { difficulty: '2' });
    await vi.waitFor(() => expect(listQuizzes).toHaveBeenCalledTimes(2));
    expect(router.currentRoute.value.query).toEqual({ difficulty: '2' });
    expect(listQuizzes).toHaveBeenLastCalledWith({ category: null, difficulty: 2 });
  });

  it('mudança de query sem mudança efectiva de filtros não faz pedido', async () => {
    await mountAt('/quizzes?category=99');
    await vi.waitFor(() => expect(host.textContent).toContain('Quiz A'));
    await router.push({ name: 'quiz-catalog', query: { category: 'abc' } });
    await router.push({ name: 'quiz-catalog', query: { category: 'abc', foo: 'bar' } });
    await settle();
    expect(listQuizzes).toHaveBeenCalledTimes(1);
  });

  it('uma resposta antiga que chega depois não substitui a mais recente', async () => {
    const first = deferred();
    const second = deferred();
    vi.mocked(listQuizzes).mockReturnValueOnce(first.promise).mockReturnValueOnce(second.promise);
    await mountAt('/quizzes');
    await router.push({ name: 'quiz-catalog', query: { category: '1' } });
    await vi.waitFor(() => expect(listQuizzes).toHaveBeenCalledTimes(2));
    second.resolve([quiz(2, 'Quiz Novo')]);
    await vi.waitFor(() => expect(host.textContent).toContain('Quiz Novo'));
    first.resolve([quiz(3, 'Quiz Antigo')]);
    await settle();
    expect(host.textContent).toContain('Quiz Novo');
    expect(host.textContent).not.toContain('Quiz Antigo');
    expect(host.querySelector('[role="status"]')).toBeNull();
  });

  it('estado vazio com filtros mostra a mensagem e "Limpar filtros" limpa a query', async () => {
    vi.mocked(listQuizzes).mockResolvedValue([]);
    await mountAt('/quizzes?category=1&difficulty=2');
    await vi.waitFor(() => expect(host.textContent).toContain('Não há quizzes para estes filtros.'));
    const button = [...host.querySelectorAll('button')].find((b) => b.textContent.trim() === 'Limpar filtros');
    button.click();
    await vi.waitFor(() => expect(listQuizzes).toHaveBeenCalledTimes(2));
    expect(router.currentRoute.value.query).toEqual({});
    expect(listQuizzes).toHaveBeenLastCalledWith({ category: null, difficulty: null });
  });

  it('ignora valores inválidos no URL', async () => {
    await mountAt('/quizzes?category=abc&difficulty=9');
    await vi.waitFor(() => expect(host.textContent).toContain('Quiz A'));
    expect(listQuizzes).toHaveBeenCalledTimes(1);
    expect(listQuizzes).toHaveBeenCalledWith({ category: null, difficulty: null });
    expect(select('Categoria').value).toBe('');
    expect(select('Dificuldade').value).toBe('');
  });

  it('sair para outra rota não faz pedido', async () => {
    await mountAt('/quizzes?category=1');
    await vi.waitFor(() => expect(host.textContent).toContain('Quiz A'));
    await router.push({ name: 'other', query: { category: '2' } });
    await settle();
    expect(host.textContent).toContain('outra');
    expect(listQuizzes).toHaveBeenCalledTimes(1);
  });
});
