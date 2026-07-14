namespace task19benchmark;

using task19;

class Program
{
    static void Main()
    {
        var scheduler = new RoundRobinScheduler();
        var processor = new CommandProcessingThread(boundedCapacity: 10, scheduler);

        var commands = new TestCommand[5];
        for (int i = 0; i < 5; i++)
        {
            commands[i] = new TestCommand(i + 1, maxExecutions: 3);
            scheduler.Add(commands[i]);
        }

        processor.Start();

        while (scheduler.HasCommand())
        {
            Thread.Sleep(10);
        }

        processor.HardStop();

        bool allFinished = true;
        foreach (var cmd in commands)
        {
            if (!cmd.IsFinished)
            {
                allFinished = false;
                break;
            }
        }

        string report = "Результаты выполнения:\n";
        report += "Количество команд: 5\n";
        report += "Требуемое количество вызовов на команду: 3\n";
        report += $"Все команды завершены: {allFinished}\n";
        report += "Выполнена демонстрация HardStop после завершения команд.\n";

        File.WriteAllText("task19_results.txt", report);
    }
}
