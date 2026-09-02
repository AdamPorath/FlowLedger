var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");

var database = postgres.AddDatabase("flowledger");

var consolidationDatabase = postgres.AddDatabase("consolidation");

var rabbitmq = builder.AddRabbitMQ("messaging");

builder.AddProject<Projects.FlowLedger_Transactions_Api>("transactions")
    .WithReference(database)
    .WithReference(rabbitmq);

builder.AddProject<Projects.FlowLedger_Consolidation_Worker>("consolidation-worker")
    .WithReference(consolidationDatabase)
    .WithReference(rabbitmq);

builder.Build().Run();