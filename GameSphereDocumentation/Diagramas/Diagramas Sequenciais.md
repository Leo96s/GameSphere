> **Efetuar Login**

```plantuml
Actor User as user
participant LoginView as LoginView
Database Database as db
participant UserController as UserController
participant HomeView as HomeView
activate user
activate LoginView
user -> LoginView : Preenche o formulário de Login
activate UserController
LoginView -> UserController : Valida e envia um Request
deactivate LoginView
activate db
UserController -> db : Valida os dados de login com a BD ValidateLogin()
alt A conta não existe ou os dados estão incorretos
    db --> UserController : Os dados não existem na BD
    deactivate db
    UserController --> LoginView : Erro "Os dados estão incorretos ou a conta não existe"
    deactivate UserController
    activate LoginView
    LoginView --> user : Mostra a mensagem de erro
    deactivate LoginView
else  
    activate UserController
    activate db
    db --> UserController : Os dados estão corretos
    deactivate db
    UserController -> LoginView : Cria a sessao do utilizador
    deactivate UserController
    activate LoginView
    activate HomeView
    LoginView -> HomeView : Redireciona para a Página Principal
    deactivate LoginView
    HomeView -> user : Mostra a Página Principal
    deactivate HomeView
    deactivate user
end
```

> **Efetuar Login com Entidade Externa (ex.Google/Facebook)**

```plantuml
actor User as user
participant LoginView as LoginView
participant ExternalProvider as ExternalProvider
database Database as db
participant UserController as UserController
participant UserModel as UserModel
participant HomeView as HomeView
activate user
activate LoginView
user -> LoginView : Clique em Login com Google/Facebook
activate ExternalProvider
LoginView -> ExternalProvider : Redireciona para o login com Google/Facebook
deactivate LoginView
user -> ExternalProvider : Indroduz as Credenciais
alt Credenciais Inválidas
    ExternalProvider --> user : Retorna erro de autenticação
else
    activate LoginView
    ExternalProvider --> LoginView : Redireciona com token de autenticação
    
end
deactivate ExternalProvider
LoginView -> UserController : Envia token para validação
deactivate LoginView
activate UserController
activate ExternalProvider
UserController -> ExternalProvider : Verifica o Token

alt Token Inválido
    ExternalProvider --> UserController : Token Inválido
    deactivate ExternalProvider
    activate LoginView
    UserController --> LoginView : Erro "Falha na Autenticação"
    LoginView --> user : Mostra a mensagem de erro
    deactivate LoginView
else
    activate ExternalProvider
    ExternalProvider --> UserController : Token Válido
    deactivate ExternalProvider
    activate db
    UserController -> db : Verifica se o Utilizador já existe
    deactivate db
    alt Utilizador não existe
        activate UserModel
        UserController --> UserModel : Modela os Dados
        UserModel --> UserController : Devolve os Dados Modelados 
        deactivate UserModel
        activate db
        UserController -> db : Cria um novo Utilizador
        deactivate db
    else    
    UserController -> LoginView : Cria uma sessão para o utilizador
    deactivate UserController
    activate LoginView
    LoginView -> HomeView : Redireciona para a Página Principal
    deactivate LoginView
    HomeView -> user : Mostra a Página Principal
end
end
deactivate user
```
