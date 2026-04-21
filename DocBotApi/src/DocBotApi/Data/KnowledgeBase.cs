using DocBotApi.Models;

namespace DocBotApi.Data;

public static class KnowledgeBase
{
    public static readonly List<DocNode> Docs = new()
    {
        new("doc-1", "Deploy", "Deploy em Homologação",
            """
            Para fazer deploy no ambiente de homologação:
            1. Acesse o Jenkins em http://jenkins.interno:8080
            2. Selecione o pipeline "deploy-homolog"
            3. Clique em "Build with Parameters"
            4. Informe a branch (ex: develop) e confirme
            5. O processo leva cerca de 8 minutos
            6. Após concluído, acesse http://homolog.interno para validar
            Obs: deploys em homolog são permitidos de segunda a sexta, das 8h às 18h.
            """),

        new("doc-2", "Deploy", "Deploy em Produção",
            """
            Deploy em produção requer aprovação do Tech Lead:
            1. Abra um PR para a branch main e aguarde revisão
            2. Após merge, crie uma tag de release: git tag v1.x.x
            3. Acesse o pipeline "deploy-prod" no Jenkins
            4. Deploys em produção só ocorrem às sextas após as 14h
            5. Monitore os logs no Grafana por 30 minutos após o deploy
            6. Em caso de falha, execute o rollback via pipeline "rollback-prod"
            """),

        new("doc-3", "Autenticação", "Configuração do IdentityServer",
            """
            O projeto usa IdentityServer4 com dois projetos:
            - commonidentity: provedor central de identidade (porta 5000)
            - oncall.identity: cliente OIDC com fluxo Hybrid
            Configurações do cliente estão no banco SQL (tabela Clients), não em Config.cs.
            Para adicionar um novo client: execute o script em /scripts/add-client.sql
            Scopes disponíveis: openid, profile, offline_access, oncall-auth, disp-webapi
            JWT secret está no appsettings via variável IS4__SigningKey
            """),

        new("doc-4", "Autenticação", "Fluxo de Login e Claims",
            """
            O fluxo de autenticação funciona assim:
            1. Usuário acessa oncall.identity que redireciona para commonidentity
            2. commonidentity valida credenciais via CadStringHash (SHA512 + salt UTF-16LE)
            3. Claims injetadas via ProfileService: isCadAuthentication, isPasswordExpired,
               passwordExpiringSoon, daysBeforePasswordExpired
            4. Token emitido com os scopes solicitados
            5. Claims chegam ao cliente via id_token (hybrid flow: code id_token)
            """),

        new("doc-5", "Banco de Dados", "Acesso ao Banco de Dados",
            """
            Bancos disponíveis:
            - SQL Server: cadhmlweb01.cicc.local (homolog) / cadprdweb01.cicc.local (prod)
            - Usuário de leitura: app_readonly | Usuário de escrita: app_readwrite
            - String de conexão está no appsettings.json via variável DB__ConnectionString
            Migrations: rode "dotnet ef database update" na pasta do projeto
            Backups: realizados diariamente às 2h, retidos por 30 dias no storage interno
            """),

        new("doc-6", "Infraestrutura", "Servidores e Ambientes",
            """
            Ambientes disponíveis:
            - Desenvolvimento: local (cada dev roda na própria máquina)
            - Homologação: cadhmlweb01.cicc.local (Windows Server 2019, IIS 10)
            - Produção: cadprdweb01.cicc.local (Windows Server 2022, IIS 10)
            Acesso via RDP: solicite ao time de infra com justificativa
            Monitoramento: Grafana em http://grafana.interno:3000
            Logs centralizados: Seq em http://seq.interno:5341
            """),

        new("doc-7", "Padrões de Código", "Logging com Serilog",
            """
            O projeto utiliza Serilog para logging:
            - Log.Information() para fluxos normais
            - Log.Warning() para situações inesperadas mas recuperáveis
            - Log.Error() para erros tratados
            - Log.Fatal() para falhas críticas que encerram o processo
            Configuração no appsettings: seção "Serilog"
            Sink padrão: Console + Seq (http://seq.interno:5341)
            Enriqueça logs com contexto: Log.ForContext<MinhaClasse>().Information(...)
            """),

        new("doc-8", "Padrões de Código", "Convenções do Projeto",
            """
            Convenções adotadas pelo time:
            - Linguagem: C# (.NET 8+), projetos web em ASP.NET Core
            - Nomenclatura: PascalCase para classes/métodos, camelCase para variáveis
            - Injeção de dependência via IServiceCollection no Program.cs
            - Nunca commitar appsettings com secrets — use variáveis de ambiente
            - Testes unitários obrigatórios para serviços críticos (cobertura mínima 70%)
            - Pull Requests: mínimo 1 aprovação do time, CI deve estar verde
            """),

        new("doc-9", "Onboarding", "Configuração do Ambiente Local",
            """
            Para configurar o ambiente local:
            1. Instale .NET 8 SDK, Visual Studio 2022 ou VS Code
            2. Clone os repos: commonidentity, oncall.identity, disp-webapi
            3. Configure o arquivo hosts: 127.0.0.1 identity.local
            4. Rode o SQL Server local via Docker: docker-compose up -d
            5. Execute as migrations: dotnet ef database update
            6. Configure appsettings.Development.json com strings de conexão locais
            7. Inicie na ordem: commonidentity → oncall.identity → aplicação
            """),

        new("doc-10", "Onboarding", "Contatos e Canais do Time",
            """
            Canais de comunicação:
            - Time de Backend: canal #backend no Teams
            - Dúvidas de Infra: canal #infra ou ticket no Jira projeto INFRA
            - Emergências em Produção: ramal 9999 ou #incidents
            - Tech Leads: Carlos (backend), Ana (frontend), Roberto (infra)
            - Reunião de Planning: toda segunda às 9h (sala virtual no Teams)
            - Code Review: PRs devem ser abertos com 2 dias de antecedência do deploy
            """),
        new("doc-11", "IdentityServer", "Como Configurar o IdentityServer",
            """
            Passos para configurar o IdentityServer4:
            1. Instalar pacotes NuGet:
               - IdentityServer4
               - IdentityServer4.EntityFramework (para ConfigurationStore via banco)
            2. Registrar no Startup.cs:
               - services.AddIdentityServer()
               - .AddConfigurationStore(...) ou .AddInMemoryClients(...)
               - .AddOperationalStore(...)
               - .AddProfileService<SeuProfileService>()
            3. Configurar Clients no banco (ou Config.cs para InMemory):
               - Definir ClientId, ClientSecrets, AllowedGrantTypes
               - Configurar RedirectUris e PostLogoutRedirectUris
               - Definir AllowedScopes (openid, profile, suas ApiScopes)
            4. Configurar Scopes:
               - IdentityResources: openid, profile
               - ApiScopes: nomes dos escopos de API (ex: oncall-auth)
               - Atenção: não duplicar nomes entre IdentityResource e ApiScope
            5. Configurar ProfileService:
               - Implementar IProfileService
               - Em GetProfileDataAsync, popular context.IssuedClaims com as claims customizadas
               - Claims injetadas no Login via AdditionalClaims do IdentityServerUser
            6. Signing Keys:
               - Dev: .AddDeveloperSigningCredential()
               - Produção: .AddSigningCredential(cert) com certificado válido
            7. Middleware:
               - app.UseIdentityServer() antes de app.UseAuthorization()
            """),
        new("doc-11", "IdentityServer", "Visão Geral da Solução",
    """
    Sistema: OnCall.Identity / CommonIdentity (IdentityServer4 + ASP.NET Core)

    Projetos:
    - src/IdentityServer              → Aplicação web principal (IdentityServer4 + UI de login + 2FA/TOTP + APIs auxiliares)
    - src/IdentityServer.Communication → Contratos e modelos compartilhados (DTOs e entidades mapeadas)

    Stack técnica:
    - .NET 8.0
    - IdentityServer4 4.1.2
    - ASP.NET Core MVC
    - Cookie Auth + OpenID Connect
    - EF Core (SQL Server)
    - Otp.NET + QRCoder (2FA/TOTP)
    - Serilog
    - DLLs Intergraph/OnCall
    """),

new("doc-12", "IdentityServer", "Arquitetura Funcional",
    """
    Camadas da aplicação:

    - Apresentação:
        Controllers MVC e views em Quickstart/Account e Views/Account

    - Autenticação / Identidade:
        IdentityServer4 com recursos/scopes/clientes em memória (Config.cs)

    - Regra de Negócio:
        Validação de credenciais CAD, política de troca de senha e 2FA

    - Persistência:
        Repositórios EF Core para usuário e TOTP

    - Integração Externa:
        OnCall (cliente OIDC), CAD Authentication e HA DB Config

    Dependências internas principais:
    - Startup.ConfigureServices  → registra IdentityServer, autenticação OIDC, filtros, cache e repositórios
    - Bootstrapper.AddInfrastructure → injeta DbContext e repositórios
    - AccountController (Quickstart) → orquestra login, troca de senha, TOTP e redirecionamento
    - TwoFactorService → gera segredo, valida código TOTP e gerencia sessões temporárias
    """),

new("doc-13", "IdentityServer", "Fluxo de Autenticação Principal",
    """
    Fluxo: OnCall.Identity → CommonIdentity → Valida Senha → TOTP → OnCall.Identity

    1. INICIAÇÃO OIDC
       - Usuário acessa OnCall.Identity (cliente OIDC externo)
       - OnCall.Identity inicia challenge para CommonIdentity (Authority)

    2. LOGIN
       - CommonIdentity abre GET /Account/Login
       - Usuário envia usuário/senha via POST /Account/Login
       - CommonIdentity valida via CADAuthentication.Authenticate(...)

    3. POLÍTICA DE SENHA
       - Se senha expirada ou troca forçada: fluxo de troca de senha
       - GET/POST /Account/ChangePassword

    4. TOTP / 2FA
       - Consulta tabela HXGN_DefinedEmployee_2fa
       - Se 2FA não configurado: exibe setup com QR Code + segredo Base32
       - Usuário informa código via POST /Account/TwoFactor
       - CommonIdentity valida com Otp.NET (janela de tolerância ±1)

    5. RETORNO AUTENTICADO
       - CommonIdentity autentica via SignInAsync
       - Redireciona ao returnUrl → usuário volta para OnCall.Identity com tokens e claims
    """),

new("doc-14", "IdentityServer", "Endpoints Principais",
    """
    Método  Rota                              Descrição
    ------  --------------------------------  -----------------------------------------------
    GET     /Account/Login                    Abre tela de login no CommonIdentity
    POST    /Account/Login                    Valida senha CAD, checa política de senha e inicia fluxo TOTP
    GET     /Account/TwoFactor?sessionKey=... Exibe tela TOTP (setup com QR ou validação normal)
    POST    /Account/TwoFactor                Valida código de 6 dígitos e conclui login
    GET     /Account/ChangePassword           Tela de troca de senha quando exigido
    POST    /Account/ChangePassword           Valida senha atual, grava nova senha e continua fluxo
    GET/POST /Account/Logout                  Efetua logout local e redireciona para OnCall
    POST    /api/Account/Authenticate         Endpoint API de validação de usuário/senha CAD
    """),

new("doc-15", "IdentityServer", "Dados e Persistência",
    """
    Tabelas utilizadas (EF Core / SQL Server):

    - DefinedEmployee
        Cadastro base do usuário, username, controle de hash de senha

    - HXGN_DefinedEmployee_2fa
        Segredo Base32 do TOTP e flag IsEnable

    Sessão temporária em memória (IMemoryCache):
    - Chaves: "2fa:{guid}" e "chgpwd:{guid}"
    - Expiração: 10 minutos
    - Usada durante fluxo de 2FA e troca de senha

    Repositórios:
    - IDefinedEmployeeRepository.GetByUsername
    - IHxgnDefinedEmployee2faRepository.GetByEmployeeIdAsync
    - IHxgnDefinedEmployee2faRepository.GetActiveByEmployeeIdAsync
    """),

new("doc-16", "IdentityServer", "Configuração OIDC, Scopes e Claims",
    """
    Grant Type atual:
    - GrantTypes.Implicit  (trechos de Hybrid/Code existem no código mas estão comentados)
    - Client principal: "OnCall" (ClientName: "TOTPUSER")
    - RedirectUri: .../oncall.identity/signin-oidc

    Identity Resources:
    - openid
    - profile
    - oncall-auth

    API Scopes:
    - api
    - disp-webapi
    - disp-realtimewebapi

    ATENÇÃO: não duplicar nomes entre IdentityResource e ApiScope.

    Claims customizadas (injetadas via AdditionalClaims no Login e propagadas pelo ProfileService):
    - isCadAuthentication
    - isPasswordExpired
    - passwordExpiringSoon
    - daysBeforePasswordExpired
    - preferred_username
    - username
    """),

new("doc-17", "IdentityServer", "Ambientes e Inicialização",
    """
    Ambientes:

    - Local:
        Usa ConnectionStrings:Connect de appsettings.Local.json

    - Demais ambientes:
        Busca conexão via HaDbConfig e aplica no EF Core na inicialização

    Ponto de entrada:
    - Program.cs

    Pipeline (Startup.Configure):
    - app.UseStaticFiles()
    - app.UseRouting()
    - app.UseIdentityServer()     // antes de UseAuthorization
    - app.UseAuthentication()
    - app.UseAuthorization()

    Logs:
    - Arquivo: c:\Temp\CommonIdentity\CommonIdentity.log
    - Console (Serilog)
    """),

new("doc-18", "IdentityServer", "Comportamentos de Segurança",
    """
    Segurança implementada:

    - Anti-forgery em todas as actions sensíveis:
        login, twofactor, changepassword, logout

    - Filtro global para AntiforgeryValidationException:
        Redireciona de forma segura em caso de token inválido

    - TOTP com código de 6 dígitos:
        Janela de verificação controlada (±1 período via Otp.NET)
        Segredo Base32 persistido por usuário na tabela HXGN_DefinedEmployee_2fa

    - Middlewares de forçamento de fluxo Hybrid:
        Existem no código mas estão desativados no pipeline atual

    - Observação operacional:
        Logs de alto detalhe e credenciais em arquivos de configuração locais
        devem ser tratados com política de segredo por ambiente
    """),

new("doc-19", "IdentityServer", "Mapa de Arquivos do Projeto",
    """
    Referências de código (mapa rápido):

    src/IdentityServer/
    ├── Program.cs
    ├── Startup.cs
    ├── Config.cs
    ├── Bootstrapper.cs
    ├── Quickstart/Account/
    │   └── AccountController.cs
    ├── Core/
    │   └── TwoFactorService.cs
    └── Repositories/
        ├── DefinedEmployee/
        │   └── DefinedEmployeeRepository.cs
        └── TwoFa/
            └── HxgnDefinedEmployee2faRepository.cs

    src/IdentityServer.Communication/
    └── Models/
        ├── DefinedEmployeeModel.cs
        └── HxgnDefinedEmployee2fa.cs
    """),
    };
}
