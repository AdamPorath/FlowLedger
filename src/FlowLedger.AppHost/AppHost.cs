var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");

var database = postgres.AddDatabase("flowledger");

var consolidationDatabase = postgres.AddDatabase("consolidation");

var rabbitmq = builder.AddRabbitMQ("messaging");

var transactionsApi = builder.AddProject<Projects.FlowLedger_Transactions_Api>("transactions-api")
    .WithReference(database)
    .WithReference(rabbitmq);

builder.AddProject<Projects.FlowLedger_Consolidation_Worker>("consolidation-worker")
    .WithReference(consolidationDatabase)
    .WithReference(rabbitmq);

var consolidationApi = builder.AddProject<Projects.FlowLedger_Consolidation_Api>("consolidation-api")
    .WithReference(consolidationDatabase);

builder.AddProject<Projects.FlowLedger_Gateway>("gateway")
    .WithReference(transactionsApi)
    .WithReference(consolidationApi);

builder.Build().Run();