using Azure.Provisioning.AppContainers;

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
    var keyVault = builder.AddAzureKeyVault("key-vault");

    jwtSigningKey = builder.AddParameter("jwt-signing-key", secret: true);
    keyVault.AddSecret("jwt-signing-key-secret", jwtSigningKey);

    var postgresUsername = builder.AddParameter("postgres-admin-username", secret: true);
    var postgresPassword = builder.AddParameter("postgres-admin-password", secret: true);
    keyVault.AddSecret("postgres-admin-password-secret", postgresPassword);

    var postgres = builder.AddAzurePostgresFlexibleServer("postgres")
        .WithPasswordAuthentication(postgresUsername, postgresPassword);

    database = postgres.AddDatabase("flowledger");
    consolidationDatabase = postgres.AddDatabase("consolidation");

    var rabbitMqConnectionString = builder.AddParameter("rabbitmq-connection-string", secret: true);
    keyVault.AddSecret("rabbitmq-connection-string-secret", rabbitMqConnectionString);

    rabbitmq = builder.AddConnectionString(
        "messaging",
        ReferenceExpression.Create($"{rabbitMqConnectionString}"));
}
else
{
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
    transactionsApi.WithEnvironment("Jwt__SigningKey", jwtSigningKey!);
    consolidationApi.WithEnvironment("Jwt__SigningKey", jwtSigningKey!);
    identityApi.WithEnvironment("Jwt__SigningKey", jwtSigningKey!);
    gateway.WithEnvironment("Jwt__SigningKey", jwtSigningKey!);

    gateway.PublishAsAzureContainerApp((_, app) => ConfigureHealthProbes(app));
    transactionsApi.PublishAsAzureContainerApp((_, app) => ConfigureHealthProbes(app));
    consolidationApi.PublishAsAzureContainerApp((_, app) => ConfigureHealthProbes(app));
    identityApi.PublishAsAzureContainerApp((_, app) => ConfigureHealthProbes(app));

    consolidationWorker.PublishAsAzureContainerApp((_, app) =>
    {
        app.Template.Scale.MinReplicas = 1;
        app.Template.Scale.MaxReplicas = 1;

        ConfigureHealthProbes(app);
    });
}

builder.Build().Run();

static void ConfigureHealthProbes(ContainerApp app)
{
    var container = app.Template.Containers[0].Value!;
    var targetPort = app.Configuration.Ingress.TargetPort;

    app.Template.TerminationGracePeriodSeconds = 45;

    container.Probes.Add(new ContainerAppProbe
    {
        ProbeType = ContainerAppProbeType.Liveness,
        HttpGet = new ContainerAppHttpRequestInfo
        {
            Path = "/alive",
            Port = targetPort,
        },
        InitialDelaySeconds = 10,
        PeriodSeconds = 15,
        FailureThreshold = 3,
        TimeoutSeconds = 5,
    });

    container.Probes.Add(new ContainerAppProbe
    {
        ProbeType = ContainerAppProbeType.Readiness,
        HttpGet = new ContainerAppHttpRequestInfo
        {
            Path = "/health",
            Port = targetPort,
        },
        InitialDelaySeconds = 5,
        PeriodSeconds = 10,
        FailureThreshold = 3,
        TimeoutSeconds = 5,
    });
}