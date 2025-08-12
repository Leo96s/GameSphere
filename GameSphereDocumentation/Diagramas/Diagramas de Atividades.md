> **Usecase 1:**

```plantuml
title Processo de Registo de Utilizadores
|Utilizador|
start

:Iniciar registo;
:Preencher informações (nome, email, palavra-passe...);
:Enviar formulário de registo;

:Validar informações;
if (Informações válidas?) then (Sim)
    :Verificar se email já está registado;
    if (Email já existe?) then (Sim)
        :Exibir erro: "Email já registado";
        |Utilizador|
        :Corrigir informações;
        -> Voltar para "Enviar formulário de registo";
    else (Não)
        :Criar conta no sistema;
        :Enviar email de confirmação;
    endif
else (Não)
    :Exibir erro: "Informações inválidas";
    |Utilizador|
    :Corrigir informações;
    -> Voltar para "Enviar formulário de registo";
endif

|Utilizador|
:Confirmar registo por email;

stop
```

> **Usecase 2:**

```plantuml
title Processo de Login de Utilizadores
|Utilizador|
start
:Iniciar Login;
:Preencher Informações (email e palavra-passe);
:Enviar formulário de autenticação;

:Validar Informações;
if (Informações Válidas com algum Utilizador Armazenado) then (Sim)
    :Autenticar Utilizador;
    :Redirecionar Utilizador para a página principal;
else (Não)
    :Exibir erro: "Palavra-Passe ou Email Incorretos";
    |Utilizador|
    :Corrigir Dados;
    ->Voltar para "Enviar formulário de autenticação";
endif
stop
```

> **Usecase 3:**

```plantuml
title Processo de Login de Utilizadores com Google/Facebook
|Utilizador|
start
:Iniciar Login com Google/Facebook;
:Selecionar Entidade (Google/Facebook);
:Redirecionamento para Página de Autenticação da Entidade;

|Google/Facebook|
:Solicitar autorização de autenticação;
if (Autorização Concedida) then (Sim)
    :Enviar Token de Autenticação para o Sistema;
else (Não)
    :Exibir erro: "Login não Autorizado";
    |Utilizador|
endif

if (Token Válido?) then (Sim)
    :Autenticar Utilizador;
    :Redirecionar para a página principal;
    stop
else (Não)
    :Exibir erro: "Falha na autenticação";
    |Utilizador|
    stop
endif
```