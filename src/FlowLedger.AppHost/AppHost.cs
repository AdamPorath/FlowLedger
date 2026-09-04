var builder = DistributedApplication.CreateBuilder(args);

var isPublishMode = builder.ExecutionContext.IsPublishMode;

// Azure Container Apps is the only deployment target modeled here. This resource has no effect
// on local `aspire run` / `dotnet run` - it only shapes `aspire publish` / `aspire deploy`.
builder.AddAzureContainerAppEnvironment("aca-env");

IResourceBuilder<ParameterResource>? jwtSigningKey = null;
IResourceBuilder<IResourceWithConnectionString> database;
IResourceBuilder<IResourceWithConnectionString> consolidationDatabase;
IResourceBuilder<IResourceWithConnectionString> rabbitmq;

if (isPublishMode)
{
    // --- Secrets (production only) ----------------------------------------------------------
    // Aspire's own secret-parameter pipeline resolves and injects these values at
    // publish/deploy time. They are additionally stored in Azure Key Vault so there is a
    // durable, auditable, rotatable record of every secret used in production, independent
    // of the deploy pipeline.
    var keyVault = builder.AddAzureKeyVault("key-vault");

    jwtSigningKey = builder.AddParameter("jwt-signing-key", secret: true);
    keyVault.AddSecret("jwt-signing-key-secret", jwtSigningKey);

    // --- PostgreSQL --------------------------------------------------------------------------
    // AddAzurePostgresFlexibleServer defaults to passwordless Microsoft Entra ID
    // authentication. The existing EF Core wiring in Transactions.Api/Consolidation.Api/
    // Consolidation.Worker uses a plain `UseNpgsql(connectionString)` call (not Aspire's
    // AddNpgsqlDbContext client integration), so it cannot consume rotating AAD tokens. Since
    // EF Core code must not be changed, password authentication is configured explicitly
    // instead of relying on the passwordless default.
    var postgresUsername = builder.AddParameter("postgres-admin-username", secret: true);
    var postgresPassword = builder.AddParameter("postgres-admin-password", secret: true);
    keyVault.AddSecret("postgres-admin-password-secret", postgresPassword);

    var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
        .WithPasswordAuthentication(postgresUsername, postgresPassword);

    database = postgres.AddDatabase("flowledger");
    consolidationDatabase = postgres.AddDatabase("consolidation");

    // --- Messaging ---------------------------------------------------------------------------
    // Aspire has no native Azure hosting integration for a managed RabbitMQ service, so the
    // production broker (e.g. CloudAMQP) is modeled as an external connection string.
    // MassTransit's `UsingRabbitMq` transport code is unchanged either way.
    var rabbitMqConnectionString = builder.AddParameter("rabbitmq-connection-string", secret: true);
    keyVault.AddSecret("rabbitmq-connection-string-secret", rabbitMqConnectionString);

    rabbitmq = builder.AddConnectionString(
        "messaging",
        ReferenceExpression.Create($"{rabbitMqConnectionString}"));
}
else
{
    // Local/dev: unchanged - plain Postgres and RabbitMQ containers, exactly as before.
    var postgres = builder.AddPostgres("postgres");
    database = postgres.AddDatabase("flowledger");
    consolidationDatabase = postgres.AddDatabase("consolidation");

    rabbitmq = builder.AddRabbitMQ("messaging");
}

var transactionsApi = builder.AddProject<Projects.FlowLedger_Transactions_Api>("transactions-api")
    .WithReference(database)
    .WithReference(rabbitmq);

var consolidationWorker = builder.AddProject<Projects.FlowLedger_Consolidation_Worker>("consolidation-worker")
    .WithReference(consolidationDatabase)
    .WithReference(rabbitmq);

var consolidationApi = builder.AddProject<Projects.FlowLedger_Consolidation_Api>("consolidation-api")
    .WithReference(consolidationDatabase);

var identityApi = builder.AddProject<Projects.FlowLedger_Identity_Api>("identity-api");

var gateway = builder.AddProject<Projects.FlowLedger_Gateway>("gateway")
    .WithReference(transactionsApi)
    .WithReference(consolidationApi)
    .WithReference(identityApi)
    .WithExternalHttpEndpoints();

if (isPublishMode)
{
    // Every service that validates or issues JWTs needs the shared signing key in production;
    // locally each service keeps reading it from its own user secrets/appsettings, unchanged.
    transactionsApi.WithEnvironment("Jwt__SigningKey", jwtSigningKey!);
    consolidationApi.WithEnvironment("Jwt__SigningKey", jwtSigningKey!);
    identityApi.WithEnvironment("Jwt__SigningKey", jwtSigningKey!);
    gateway.WithEnvironment("Jwt__SigningKey", jwtSigningKey!);

    // Consolidation.Worker has no HTTP endpoint/ingress at all (Generic Host, no Kestrel) and
    // therefore no autoscale signal to ever wake it back up once scaled to zero. Force a
    // permanently-running single replica so queued messages are always consumed. The four
    // HTTP-facing services (gateway, transactions-api, consolidation-api, identity-api) are
    // left at the Azure Container Apps Consumption-plan default (MinReplicas = 0), so they
    // scale to zero when idle and scale up on incoming HTTP traffic.
    consolidationWorker.PublishAsAzureContainerApp((_, app) =>
    {
        app.Template.Scale.MinReplicas = 1;
        app.Template.Scale.MaxReplicas = 1;
    });
}

builder.Build().Run();