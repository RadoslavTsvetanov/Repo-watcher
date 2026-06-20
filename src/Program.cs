using GitManagerApp;
using GitManagerApp.AI;
using GitManagerApp.Configuration;

var config = ManagerConfig.Default(Directory.GetCurrentDirectory());

using var manager = new GitManager(config, new DummyAIService());
manager.Start();

Console.WriteLine("Git Manager running. Press Ctrl+C to exit.");
Thread.Sleep(Timeout.Infinite);
