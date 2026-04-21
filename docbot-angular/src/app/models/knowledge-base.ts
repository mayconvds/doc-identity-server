import { Doc } from '../models/chat.model';

export const DOCS: Doc[] = [
  {
    id: 1,
    category: 'Deploy',
    title: 'Deploy em Homologação',
    content: `Para fazer deploy no ambiente de homologação:
1. Acesse o Jenkins em http://jenkins.interno:8080
2. Selecione o pipeline "deploy-homolog"
3. Clique em "Build with Parameters"
4. Informe a branch (ex: develop) e confirme
5. O processo leva cerca de 8 minutos
6. Após concluído, acesse http://homolog.interno para validar
Obs: deploys em homolog são permitidos de segunda a sexta, das 8h às 18h.`,
  },
  {
    id: 2,
    category: 'Deploy',
    title: 'Deploy em Produção',
    content: `Deploy em produção requer aprovação do Tech Lead:
1. Abra um PR para a branch main e aguarde revisão
2. Após merge, crie uma tag de release: git tag v1.x.x
3. Acesse o pipeline "deploy-prod" no Jenkins
4. Deploys em produção só ocorrem às sextas após as 14h ou em janelas emergenciais aprovadas
5. Monitore os logs no Grafana por 30 minutos após o deploy
6. Em caso de falha, execute o rollback via pipeline "rollback-prod"`,
  },
  {
    id: 3,
    category: 'Autenticação',
    title: 'Configuração do IdentityServer',
    content: `O projeto usa IdentityServer4 com dois projetos:
- commonidentity: provedor central de identidade (porta 5000)
- oncall.identity: cliente OIDC com fluxo Hybrid
Configurações do cliente estão no banco SQL (tabela Clients), não em Config.cs.
Para adicionar um novo client: execute o script em /scripts/add-client.sql
Scopes disponíveis: openid, profile, offline_access, oncall-auth, disp-webapi
JWT secret está no appsettings via variável IS4__SigningKey`,
  },
  {
    id: 4,
    category: 'Autenticação',
    title: 'Fluxo de Login e Claims',
    content: `O fluxo de autenticação funciona assim:
1. Usuário acessa oncall.identity que redireciona para commonidentity
2. commonidentity valida credenciais via CadStringHash (SHA512 + salt UTF-16LE)
3. Claims customizadas são injetadas via ProfileService: isCadAuthentication, isPasswordExpired, passwordExpiringSoon, daysBeforePasswordExpired
4. Token é emitido com os scopes solicitados
5. Claims chegam ao cliente via id_token (hybrid flow: code id_token)`,
  },
  {
    id: 5,
    category: 'Banco de Dados',
    title: 'Acesso ao Banco de Dados',
    content: `Bancos disponíveis:
- SQL Server: cadhmlweb01.cicc.local (homolog) / cadprdweb01.cicc.local (prod)
- Usuário de leitura: app_readonly | Usuário de escrita: app_readwrite
- String de conexão está no appsettings.json via variável DB__ConnectionString
Migrations: rode "dotnet ef database update" na pasta do projeto
Backups: realizados diariamente às 2h, retidos por 30 dias no storage interno`,
  },
  {
    id: 6,
    category: 'Infraestrutura',
    title: 'Servidores e Ambientes',
    content: `Ambientes disponíveis:
- Desenvolvimento: local (cada dev roda na própria máquina)
- Homologação: cadhmlweb01.cicc.local (Windows Server 2019, IIS 10)
- Produção: cadprdweb01.cicc.local (Windows Server 2022, IIS 10)
Acesso via RDP: solicite ao time de infra com justificativa
Monitoramento: Grafana em http://grafana.interno:3000
Logs centralizados: Seq em http://seq.interno:5341`,
  },
  {
    id: 7,
    category: 'Padrões de Código',
    title: 'Logging com Serilog',
    content: `O projeto utiliza Serilog para logging:
- Log.Information() para fluxos normais
- Log.Warning() para situações inesperadas mas recuperáveis
- Log.Error() para erros tratados
- Log.Fatal() para falhas críticas que encerram o processo
Configuração no appsettings: seção "Serilog"
Sink padrão: Console + Seq (http://seq.interno:5341)
Enriqueça logs com contexto: Log.ForContext<MinhaClasse>().Information(...)`,
  },
  {
    id: 8,
    category: 'Padrões de Código',
    title: 'Convenções e Padrões do Projeto',
    content: `Convenções adotadas pelo time:
- Linguagem: C# (.NET 6+), projetos web em ASP.NET Core
- Nomenclatura: PascalCase para classes/métodos, camelCase para variáveis
- Injeção de dependência via IServiceCollection no Startup/Program.cs
- Nunca commitar appsettings com secrets — use variáveis de ambiente ou Secrets Manager
- Testes unitários obrigatórios para serviços críticos (cobertura mínima 70%)
- Pull Requests: mínimo 1 aprovação do time, CI deve estar verde`,
  },
  {
    id: 9,
    category: 'Onboarding',
    title: 'Configuração do Ambiente de Desenvolvimento',
    content: `Para configurar o ambiente local:
1. Instale .NET 6 SDK, Visual Studio 2022 ou VS Code
2. Clone os repos: commonidentity, oncall.identity, disp-webapi
3. Configure o arquivo hosts: 127.0.0.1 identity.local
4. Rode o SQL Server local via Docker: docker-compose up -d
5. Execute as migrations: dotnet ef database update
6. Configure appsettings.Development.json com as strings de conexão locais
7. Inicie na ordem: commonidentity → oncall.identity → aplicação`,
  },
  {
    id: 10,
    category: 'Onboarding',
    title: 'Contatos e Canais do Time',
    content: `Canais de comunicação:
- Time de Backend: canal #backend no Teams
- Dúvidas de Infra: canal #infra ou abra ticket no Jira projeto INFRA
- Emergências em Produção: ligue para o plantão (ramal 9999) ou use #incidents
- Tech Leads: Carlos (backend), Ana (frontend), Roberto (infra)
- Reunião de Planning: toda segunda às 9h (sala virtual no Teams)
- Code Review: PRs devem ser abertos com pelo menos 2 dias de antecedência do deploy`,
  },
];

export const CATEGORY_COLORS: Record<string, string> = {
  'Deploy': '#f97316',
  'Autenticação': '#a78bfa',
  'Banco de Dados': '#34d399',
  'Infraestrutura': '#60a5fa',
  'Padrões de Código': '#f472b6',
  'Onboarding': '#fbbf24',
};

export const SUGGESTIONS: string[] = [
  'Como fazer deploy em homologação?',
  'Como configurar o IdentityServer?',
  'Onde vejo os logs da aplicação?',
  'Como configuro meu ambiente local?',
  'Quais são as convenções de código?',
];
