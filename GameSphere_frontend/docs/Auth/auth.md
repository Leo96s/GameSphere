# 🔐 Sistema de Autenticação

O GameSphere utiliza um sistema híbrido de autenticação, integrando **Firebase Social Login** e um **Backend próprio via JWT**.

## 🚀 Fluxo de Login
Atualmente, o projeto suporta o login manual e está preparado para expansão social.

1. **Submissão:** O utilizador insere as credenciais no componente `Login.vue`.
2. **Serviço:** O `authService.ts` comunica com a API (Axios).
3. **Persistência:** Após o sucesso, o Token JWT é armazenado no `localStorage` e os dados do utilizador são injetados no estado global.

## 🛠 Persistência e Sincronização
Como o `localStorage` por si só não é reativo no Vue 3, utilizamos um sistema de eventos para garantir que o **Header** e a **Sidebar** se atualizam instantaneamente.

### Exemplo de Implementação:
```javascript
// Local: src/services/authService.ts
export const login = async (credentials) => {
  const response = await api.post('/login', credentials);
  localStorage.setItem('user_token', response.data.token);

  // Dispara um evento para o resto da app acordar
  window.dispatchEvent(new Event("user-logged-in"));
  return response.data;
};

```

## 🛡 Proteção de Rotas (Navigation Guards)

As rotas que exigem autenticação (como o Perfil) são protegidas no `router.js`.

| Rota | Acesso | Condição |
| --- | --- | --- |
| `/login` | Público | Redireciona se já estiver logado |
| `/profile` | Privado | Redireciona para login se sem token |
| `/register` | Público | Aberto a novos utilizadores |

## 🧩 Componentes Relacionados

Podes ver a documentação visual destes componentes no nosso **Storybook**:

* `LoginCard`: Interface do formulário.
* `AppHeader`: Reage ao estado de `isLoggedIn`.

> [!TIP]
> **Segurança:** Nunca armazenes a password em texto limpo no estado do componente. Usa sempre o fluxo de envio direto para o serviço de API.

---

### Por que isto é útil?

1.  **Tabela de Rotas:** Visualizas rapidamente quem pode aceder a quê sem ler 200 linhas de `router.js`.
2.  **Bloco de Código:** Se precisares de replicar o evento em outra parte da app, o código está ali à mão.
3.  **Dica (TIP):** Ajuda a manter boas práticas de segurança no projeto.

### Próximo Passo:

Para que esta página apareça no teu VitePress, não te esqueças de adicioná-la à **Sidebar** no teu `config.mts`:

```typescript
// No config.mts
items: [
  { text: 'Autenticação', link: '/auth' }, // link para o ficheiro auth.md
  // ...
]

