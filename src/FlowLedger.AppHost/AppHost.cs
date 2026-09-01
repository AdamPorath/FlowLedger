var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithHostPort(55432);;

var database = postgres.AddDatabase("flowledger");

builder.AddProject<Projects.FlowLedger_Transactions_Api>("transactions")
    .WithReference(database);

builder.Build().Run();