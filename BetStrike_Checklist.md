# BetStrike - Checklist do Projeto

## 1. Base de Dados `Apostas` e Stored Procedures
- [X] **Criar a Base de Dados `Apostas`.**
- [X] **Tabela `Jogo`**: 
  - [X] Campos: ID (AI PK), Código (Uniquekey, `FUT-AAAA-JJNN`), Data/Hora, Equipa Casa, Equipa Fora, Competição, Estado da Partida (1 a 5).
- [X] **Tabela `Resultado`**: 
  - [X] Campos: FK para `Jogo` (1-para-1 com delete cascade), Golos Casa, Golos Fora, Data/Hora da última atualização.
- [X] **Tabela `Aposta`**: 
  - [X] Campos: FK Jogo, ID Utilizador, Tipo (`1`, `X`, `2`), Montante, Odd do momento, Estado (1 a 4), Data/Hora.
- [X] **Criar Stored Procedures (SP) para ACESSO EXCLUSIVO (sem SQL inline na API)**:
  - [X] Inserir Jogo (validar formato e duplicados).
  - [X] Atualizar estado e resultado. Se mudar para Finalizado sem resultado inserido, forçar a criação de um registo com `0-0` (desconhecido).
  - [X] Inserir Aposta (validar tipo `1/X/2`, valor > 0, odd > 1.0, saldo suficiente e garantir que o estado do jogo não é 3, 4 ou 5).
  - [X] Cancelar Aposta.
  - [X] Resolver Apostas de jogo finalizado (Lógica do resultado final com atribuição de estados ganha/perdida/anulada).
  - [X] Inserir Resultado (apenas estado 3, erro se já houver um segundo resultado).
  - [X] Consultas (Jogos com filtros, Apostas por utilizador, Estatísticas).

## 2. Base de Dados `Pagamentos` e Integração
- [X] **Criar a Base de Dados `Pagamentos`.**
- [X] **Tabela `Transacao`**: 
  - [X] Campos: Ref. Aposta, ID Utilizador, Tipo (`AP`, `PG`, `RE`, `DE`, `LV`), Valor, Data/Hora, Estado.
- [X] **Tabela `Saldo_Utilizador`**: 
  - [X] Campos: ID Utilizador (Único), Saldo Atual, Data/Hora Atualização. Garantir que saldo nunca é negativo na BD.
- [X] **Criar Triggers de Integração na BD `Apostas`**:
  - [X] Ao atualizar o estado da Aposta (1 -> 2): Inserir transação `PG` (Valor * Odd) e atualizar o `Saldo_Utilizador`.
  - [X] Ao atualizar o estado da Aposta (1 -> 4): Inserir transação `RE` (Valor apostado devolvido) e atualizar o `Saldo_Utilizador`.
  - [X] Ao criar a Aposta: Inserir a transação original de débito (`AP`).
  - [X] Procedimento ou Trigger para quando o Jogo passar para cancelado ou adiado, anular as respetivas apostas pendentes.

## 3. Plataforma de Resultados de Futebol (API REST)
- [X] **Criar projeto da API REST.**
- [X] **Endpoint POST**: Inserir novo jogo. Retorna ID e rejeita duplicação.
- [X] **Endpoint PUT/PATCH**: Atualizar estado/marcador de jogo existente (com validação de transição de estado).
- [X] **Endpoint GET**: Listar jogos com query strings opcionais de data e estado.
- [X] **Endpoint GET `{id}`**: Devolver dados e placar de um jogo específico.
- [X] **Endpoint DELETE `{id}`**: Remover jogo (só se o estado for `1 - Agendado`).

## 4. Plataforma de Gestão de Apostas (API REST)
- [X] **Criar projeto da API REST e integrar com Stored Procedures de `Apostas`.**
- [X] **Gestão de Utilizadores**: 
  - [X] Criar endpoint de registo (Processo atómico: Criar utilizador e saldo inicial de `50.00` em `Pagamentos`).
- [X] **Gestão de Apostas**:
  - [X] POST para registar aposta (debita saldo imediatamente).
  - [X] GET com vários filtros (utilizador, jogo, estado, datas).
  - [X] GET detalhe da aposta e prémio potencial.
  - [X] POST/DELETE para cancelar aposta pendente num jogo `Agendado` (e reembolsar o valor).
- [X] **Gestão de Jogos**:
  - [X] Integração para guardar/atualizar dados provindos da API de Resultados.
  - [X] Resolver apostas autonomamente sempre que estado mude para finalizado.
- [X] **Gestão de Resultados e Estatísticas**:
  - [X] Endpoints para colocar e resgatar o resultado final.
  - [X] Estatísticas isoladas por Jogo e Agregadas (por Competição).
- [X] **Ferramenta de Testes**: Endpoint para Depósito de dinheiro fictício em `Pagamentos`.

## 5. Aplicação Geradora de Dados (Consola ou GUI)
- [X] **Lógica de Partidas/Jornadas**:
  - [X] Algoritmo de emparelhamento 1ª Liga (9 jogos gerados no arranque). Regra de não duplicar e jogar uma em casa / outra fora em jornadas consecutivas.
  - [X] Gerador do ID Automático: `FUT-AAAA-JJNN`.
- [X] **Motor de Simulação Em Paralelo (Threads/Tasks)**:
  - [X] Simular os 9 jogos ao mesmo tempo.
  - [X] Updates cronometrados (intervalos de 10s = 10 minutos simulados).
  - [X] Flow de Estados: `Agendado(1) -> Em Curso(2) -> Finalizado(3)`.
  - [X] Simulação de marcadores aleatórios durante `Em Curso`(Média de 2-3 golos por jogo).
- [X] **Comunicação de Saída**:
  - [X] Chamada de POST para a **Plataforma de Resultados** ao criar jogos.
  - [X] Chamada de PUT para a **Plataforma de Resultados** a cada 10 segundos quando o estado ou número de golos alterar.

## 6. Parte Exploratória — Containerização da Infraestrutura
- [ ] **Dockerizar Serviços**:
  - [ ] Criar ficheiro `Dockerfile` para a API de Resultados de Futebol.
  - [ ] Criar ficheiro `Dockerfile` para a API de Gestão de Apostas.
  - [ ] Criar ficheiro `Dockerfile` (se viável) para a App Geradora.
- [ ] **Orquestração com Docker Compose**:
  - [ ] Criar ficheiro `docker-compose.yml`.
  - [ ] Configurar um container para a / as Bases de Dados (e.g., SQL Server image).
  - [ ] Setar env vars nas APIs e mapear as portas (ports) de comunicação local.
  - [ ] Testar `docker-compose up` para validar o start fluido de todos os componentes da infraestrutura de uma só vez.