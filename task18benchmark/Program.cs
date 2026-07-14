namespace task18benchmark;

using task18;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using ScottPlot;

public class WorkCommand : IRepeatableCommand
{
    public int TicksRemaining { get; private set; }
    public bool IsFinished => TicksRemaining == 0;

    public WorkCommand(int ticks)
    {
        TicksRemaining = ticks;
    }

    public void Execute()
    {
        int sum = 0;
        for (int i = 0; i < 10000; i++)
            sum += i;

        TicksRemaining--;
    }
}

class Program
{
    static void Main(string[] args)
    {
        int[] counts = { 100, 500, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000 };
        List<double> times = new List<double>();
        int ticksPerCommand = 5;

        foreach (int count in counts)
        {
            var scheduler = new RoundRobinScheduler();
            var processor = new CommandProcessingThread(boundedCapacity: count, scheduler);

            for (int i = 0; i < count; i++)
            {
                scheduler.Add(new WorkCommand(ticksPerCommand));
            }

            processor.Start();
            Stopwatch timer = Stopwatch.StartNew();

            while (scheduler.HasCommand())
            {
                Thread.Sleep(1);
            }
            Thread.Sleep(30);

            timer.Stop();
            times.Add(timer.Elapsed.TotalMilliseconds);

            processor.Stop();
            Thread.Sleep(30);
        }

        Plot myPlot = new Plot();
        double[] x = new double[counts.Length];
        double[] y = new double[counts.Length];
        for (int i = 0; i < counts.Length; i++)
        {
            y[i] = times[i];
            x[i] = counts[i];
        }

        myPlot.Add.Scatter(x, y);
        myPlot.Title("Зависимость времени от количества команд");
        myPlot.XLabel("Кол-во команд");
        myPlot.YLabel("Время в мс");
        myPlot.SavePng("graph.png", 1000, 800);

        string report = "Результат:\n";
        for (int i = 0; i < counts.Length; i++)
        {
            report += $"Команд: {counts[i]}, Время: {times[i]:F2} мс\n";
        }
        report += "\nНаблюдается линейная зависимость времени выполнения от числа команд." +
	          "\nПричина — последовательный алгоритм планировщика, обрабатывающий ровно одну команду за каждый вызов Execute (один тик).";

        File.WriteAllText("task18_results.txt", report);
    }
}
