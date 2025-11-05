using Aspire.Hosting;
using FlowRules.AspireHost.AppHost;
using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

using ActivityConsoleListener activityListener = new(s =>
    s.Name.StartsWith("FlowRules") 
    || s.Name == builder.Environment.ApplicationName);

builder.AddProject<FlowRules_Samples_WebApi>("webapi");

builder.Build().Run();
