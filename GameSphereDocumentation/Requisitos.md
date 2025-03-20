#documentação #requisitos

>Níveis de Prioridade:
>1- Baixa
>2- Média
>3- Desejável
>4- Importante
>5- Essencial



| Requisito         |                                                                                                                                                                                                                                     |
| ----------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Categoria         | Gestão de Utilizadores                                                                                                                                                                                                              |
| ID                | RF - 1.1                                                                                                                                                                                                                            |
| Nome do Requisito | Registo de Utilizadores                                                                                                                                                                                                             |
| Descrição         | Permite que novos utilizadores se registem na aplicação preenchendo um formulário com nome, e-mail, palavra-passe, gênero e confirmação de palavra-passe. O sistema deve garantir a unicidade do e-mail e validar a força da senha. |
| Prioridade        | 3- Desejável                                                                                                                                                                                                                        |
| Estado            | Fixo                                                                                                                                                                                                                                |
| Restrições        | O e-mail deve ser único no sistema. Palavras-passes precisam ter no mínimo 8 caracteres, incluindo letras, números e caracteres especiais.                                                                                          |
| Verificação       | Um utilizador consegue se registar com os dados corretos, e recebe um e-mail de confirmação.                                                                                                                                        |

| Requisito         |                                                                                                                                                                               |
| ----------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Categoria         | Gestão de Utilizadores                                                                                                                                                        |
| ID                | RF - 1.2                                                                                                                                                                      |
| Nome do Requisito | Login de Utilizadores                                                                                                                                                         |
| Descrição         | Permite que utilizadores façam login na aplicação utilizando o e-mail e palavra-passe com que se cadastraram. O sistema deve validar as credenciais e garantir acesso seguro. |
| Prioridade        | 3- Desejável                                                                                                                                                                  |
| Estado            | Fixo                                                                                                                                                                          |
| Restrições        | O sistema deve proteger contra tentativas repetidas de login, ao fim de 5 tentativas falhadas deve bloquear o acesso ao sistema temporariamente.                              |
| Verificação       | O utilizador insere as credenciais corretas e consegue fazer login com sucesso. Se as credenciais forem incorretas, o sistema apresenta uma mensagem de erro.                 |

| Requisito         |                                                                                                                                                          |
| ----------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Categoria         | Gestão de Utilizadores                                                                                                                                   |
| ID                | RF - 1.3                                                                                                                                                 |
| Nome do Requisito | Recuperação de Palavra-passe                                                                                                                             |
| Descrição         | Permite que utilizadores que esqueceram a palavra-passe solicitem a redefinição. O sistema envia um e-mail com um código para redefinir a palavra-passe. |
| Prioridade        | 3- Desejável                                                                                                                                             |
| Estado            | Fixo                                                                                                                                                     |
| Restrições        | O link de redefinição deve expirar após um período definido de 24h.                                                                                      |
| Verificação       | O utilizador insere o e-mail no formulário de recuperação, recebe um e-mail com o código, redefine a senha com sucesso e faz login.                      |

| Requisito         |                                                                                                                                                                                                                                    |
| ----------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Categoria         | Gestão de Utilizadores                                                                                                                                                                                                             |
| ID                | RF - 1.4                                                                                                                                                                                                                           |
| Nome do Requisito | Login com Redes Sociais                                                                                                                                                                                                            |
| Descrição         | Permite que utilizadores façam login na aplicação android e site utilizando contas de redes sociais, exemplo, Google, Facebook, entre outros. O sistema deve integrar com as APIs dessas plataformas para realizar a autenticação. |
| Prioridade        | 2- Média                                                                                                                                                                                                                           |
| Estado            | Fixo                                                                                                                                                                                                                               |
| Restrições        | As credenciais das redes sociais devem ser validadas corretamente, e o sistema deve garantir a proteção dos dados de autenticação.                                                                                                 |
| Verificação       | O utilizador consegue fazer login através de uma conta de rede social, e o sistema valida as credenciais com sucesso.                                                                                                              |

| Requisito         |                                                                                                                                                                                                                 |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Categoria         | Gestão de Utilizadores                                                                                                                                                                                          |
| ID                | RF - 1.5                                                                                                                                                                                                        |
| Nome do Requisito | Gestão de Sessões de Utilizadores                                                                                                                                                                               |
| Descrição         | O sistema deve manter a sessão do utilizador ativa após o login, permitindo que ele navegue sem precisar inserir as credenciais repetidamente. Deve ser possível fazer logout da aplicação, encerrando a sessão |
| Prioridade        | 3- Desejável                                                                                                                                                                                                    |
| Estado            | Fixo                                                                                                                                                                                                            |
| Restrições        | A sessão deve expirar após um tempo de inatividade de 30 minutos ou após o encerramento manual pelo utilizador.                                                                                                 |
| Verificação       | Após o login, a sessão do utilizador mantém-se ativa até ele sair ou a sessão expirar. O utilizador pode fazer logout, encerrando a sessão.                                                                     |

| Requisito         |                                                                       |
| ----------------- | --------------------------------------------------------------------- |
| Categoria         | Gestão de Perfil                                                      |
| ID                | RF - 2.1                                                              |
| Nome do Requisito | Aceder ao perfil                                                      |
| Descrição         | Deve ser possível aceder aos dados pessoais                           |
| Prioridade        | 4- Importante                                                         |
| Estado            | Fixo                                                                  |
| Restrições        | Acesso restrito ao próprio utilizador ou administradores autorizados. |
| Verificação       | Verificação de conformidade com as normas de proteção de dados.       |

| Requisito         |                                                                                                                                                                                                                                                   |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Categoria         | Gestão de Perfil                                                                                                                                                                                                                                  |
| ID                | RF - 2.2                                                                                                                                                                                                                                          |
| Nome do Requisito | Editar perfil                                                                                                                                                                                                                                     |
| Descrição         | Deve ser possível editar os dados pessoais de forma a poder ser possível mantê-los sempre atualizados.                                                                                                                                            |
| Prioridade        | 4- Importante                                                                                                                                                                                                                                     |
| Estado            | Fixo                                                                                                                                                                                                                                              |
| Restrições        | Apenas o próprio utilizador ou administradores autorizados podem editar os dados. Validação dos dados inseridos para garantir precisão e conformidade com as normas de proteção de dados                                                          |
| Verificação       | Testes de edição para garantir que as alterações sejam salvas corretamente. Verificação de validação de campos obrigatórios e formatos específicos. Testes de permissão para garantir que apenas utilizadores autorizados possam editar os dados. |

| Requisito         |                                                                                                                                                                                                                                                     |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Categoria         | Gestão de Perfil                                                                                                                                                                                                                                    |
| ID                | RF - 2.3                                                                                                                                                                                                                                            |
| Nome do Requisito | Eliminar perfil                                                                                                                                                                                                                                     |
| Descrição         | Deve ser possível eliminar perfil caso o utilizador assim o decida fazer.                                                                                                                                                                           |
| Prioridade        | 2- Média                                                                                                                                                                                                                                            |
| Estado            | Fixo                                                                                                                                                                                                                                                |
| Restrições        | Confirmação explícita do usuário antes da exclusão do perfil. Dados críticos podem ser mantidos por um período determinado para fins legais ou de auditoria                                                                                         |
| Verificação       | Testes de confirmação para garantir que o perfil só seja excluído após a confirmação do utilizador. Verificação de conformidade com as normas de proteção de dados. Testes para garantir que os dados sejam excluídos ou anonimizados corretamente. |

| Requisito         |                                                                                                             |
| ----------------- | ----------------------------------------------------------------------------------------------------------- |
| Categoria         | Gestão de Perfil                                                                                            |
| ID                | RF - 2.4                                                                                                    |
| Nome do Requisito | Gestão de Preferências                                                                                      |
| Descrição         | O utilizador deve poder configurar preferências pessoais, como idioma, notificações e temas (claro/escuro). |
| Prioridade        | 3- Desejável                                                                                                |
| Estado            | Fixo                                                                                                        |
| Restrições        | As preferências devem ser salvas e aplicadas em todas as sessões do utilizador.                             |
| Verificação       | Testes para garantir que as preferências sejam salvas e aplicadas corretamente.                             |

| Requisito         |                                                                                                              |
| ----------------- | ------------------------------------------------------------------------------------------------------------ |
| Categoria         | Gestão de Perfil                                                                                             |
| ID                | RF - 2.5                                                                                                     |
| Nome do Requisito | Visualização de Atividades Recentes                                                                          |
| Descrição         | O utilizador deve poder visualizar suas atividades recentes na plataforma (ex: logins, alterações de dados). |
| Prioridade        | 4- Importante                                                                                                |
| Estado            | Fixo                                                                                                         |
| Restrições        | Apenas o próprio utilizador deve ter acesso a essas informações.                                             |
| Verificação       | Testes para garantir que as atividades sejam registradas e exibidas corretamente.                            |

| Requisito         |     |
| ----------------- | --- |
| Categoria         |     |
| ID                |     |
| Nome do Requisito |     |
| Descrição         |     |
| Prioridade        |     |
| Estado            |     |
| Restrições        |     |
| Verificação       |     |