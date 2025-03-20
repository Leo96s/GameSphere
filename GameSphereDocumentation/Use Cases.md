## <font color="#ff0000">UseCase-1</font>

```plantuml
left to right direction
actor "Utilizador " as Utilizador 
rectangle Uso_de_Registo_de_Utilizadores {
  usecase "Registo" as UC1
  usecase "Validação e Confirmação de Email" as UC2
}
Utilizador -d- UC1
UC1 .d.> UC2 : <<include>>
```

| Use case Name       | Registo do Utilizador                                                                                                                                                                                                                                                                       |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Short Description   | Permite ao utilizador registar-se na Plataforma.                                                                                                                                                                                                                                            |
| Actors              | User                                                                                                                                                                                                                                                                                        |
| Pré-conditions      | O utilizador ter um email criado previamente.                                                                                                                                                                                                                                               |
| Typical flow        | 1º O Utilizador introduz Nome, Email, Palavra-Passe e confirma a Palavra-Passe no formulário; 2º O sistema valida os dados e verifica a unicidade do e-mail e valida a força da senha; 3º O registo é bem sucedido e o Utilizador recebe um email para confirmar a Conta.                   |
| Alternative flow    | 1º O Utilizador introduz Nome, Email, Palavra-Passe e confirma a Palavra-Passe no formulário; 2º O sistema valida os dados e verifica a unicidade do e-mail e valida a força da senha; 3º O registo não é bem sucedido e o Utilizador é informado que já existe uma conta com aquele email. |
| Use case extensions | N.A                                                                                                                                                                                                                                                                                         |
| Post-conditions     | O utilizador passa a estar registado na Plataforma.                                                                                                                                                                                                                                         |

## <font color="#ff0000">UseCase-2</font>

```plantuml
left to right direction
actor "Utilizador " as Utilizador 
rectangle Uso_de_Login_de_Utilizadores {
  usecase "Login" as UC1
  usecase "Recuperação de Palavra-Passe" as UC2
  usecase "Gestão de Sessões de Utilizador" as UC3
}
Utilizador -d- UC1
UC1 <.d. UC2 : <<extend>>
UC1 .d.> UC3 : <<include>>
```

| Use case Name | Login do Utilizador |
|---------------|---------------------|
| Short Description | Permite ao utilizador autenticar-se na Plataforma. |
| Actors | User |
| Pré-conditions | O utilizador ter uma conta na plataforma criada previamente. |
| Typical flow | 1º O Utilizador introduz Email e Palavra-Passe no formulário; 2º O sistema valida as credenciais; 3º A autenticação é bem sucedida e o Utilizador é redirecionado para a página principal. |
| Alternative flow | 1º O Utilizador introduz Email e Palavra-Passe no formulário; 2º O sistema valida as credenciais; 3º A autenticação não é bem sucedida e o Utilizador é informado que a Palavra-Passe ou Email estão incorretos. |
| Use case extensions | Recuperação de Palavra-Passe |
| Post-conditions | O utilizador passa a estar autenticado na Plataforma. |

## <font color="#ff0000">UseCase-3</font>

```plantuml
left to right direction
actor "Utilizador " as Utilizador 
actor "Google" as google
actor "Facebook" as facebook
rectangle Uso_de_Login_de_Utilizadores_Google/Facebook {
  usecase "Login com Redes Sociais" as UC1
  usecase "Gestão de Sessões de Utilizador" as UC2
}
Utilizador -d- UC1
google -d- UC1
facebook -d- UC1
UC1 .d.> UC2 : <<include>>
```

| Use case Name | Login do Utilizador pelo Google/Facebook |
|---------------|------------------------------------------|
| Short Description | Permite ao utilizador autenticar-se na Plataforma através do Google/Facebook. |
| Actors | User, Google, Facebook |
| Pré-conditions | O utilizador ter uma conta Google/Facebook criada previamente. |
| Typical flow | 1º O Utilizador seleciona por qual das entidades deseja se autenticar; 2º O utilizador é redirecionado para a página de autenticação da entidade; 3º A autenticação é efetuada com sucesso; 4º O utilizador é redirecionado para a página principal da plataforma. |
| Alternative flow | 1º O Utilizador seleciona por qual das entidades deseja se autenticar; 2º O utilizador é redirecionado para a página de autenticação da entidade; 3º A autenticação não é efetuada; 4º O utilizador é informado que ocorreu um erro ao tentar-se autenticar com aquela entidade. |
| Use case extensions | N.A |
| Post-conditions | O utilizador passa a estar autenticado na Plataforma. |
