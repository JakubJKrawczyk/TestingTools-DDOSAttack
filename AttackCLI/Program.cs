using RestSharp;
using System.Diagnostics;
using System.Reflection;

class Program
{
    private static readonly object ConsoleLock = new();
    private static DateTime EndTime;
    private static long TotalRequests = 0;
    private static long FailedRequests = 0;
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    private static byte[] ImageBytes;
    private static readonly HashSet<string> UniqueErrors = new();
    private static readonly object ErrorsLock = new();

    static async Task Main()
    {
        Console.WriteLine("API Stress Test Tool");

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(str => str.EndsWith("image.jpg"));

            if (resourceName == null)
            {
                Console.WriteLine("Embedded image resource not found!");
                return;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Console.WriteLine("Could not load image resource!");
                return;
            }

            ImageBytes = new byte[stream.Length];
            await stream.ReadAsync(ImageBytes, 0, (int)stream.Length);
            
            Console.WriteLine($"Loaded image resource: {resourceName} ({ImageBytes.Length / 1024} KB)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading embedded image: {ex.Message}");
            return;
        }

        string serverUrl;
        while (true)
        {
            Console.Write("Enter server IP or URL (e.g. 185.92.219.22 or api.example.com): ");
            var input = Console.ReadLine()?.Trim();
            
            if (!input.StartsWith("http://") && !input.StartsWith("https://"))
            {
                input = "http://" + input;
            }
            
            if (Uri.TryCreate(input, UriKind.Absolute, out Uri? uri))
            {
                serverUrl = uri.ToString();
                break;
            }
            else
            {
                Console.WriteLine("Invalid URL format. Please try again.");
            }
        }

        Console.Write("Enter endpoint path (press Enter for root '/'): ");
        var path = Console.ReadLine()?.Trim() ?? "/";
        if (!path.StartsWith("/")) path = "/" + path;
        
        serverUrl = serverUrl.TrimEnd('/') + path;
        Console.WriteLine($"Full URL: {serverUrl}");

        Console.Write("Enter test duration in seconds: ");
        if (!int.TryParse(Console.ReadLine(), out int duration))
        {
            Console.WriteLine("Invalid duration. Exiting.");
            return;
        }

        Console.Write("Enter number of concurrent threads (recommended 10-50): ");
        if (!int.TryParse(Console.ReadLine(), out int threadCount))
        {
            threadCount = 10;
            Console.WriteLine("Invalid thread count. Using default: 10");
        }

        EndTime = DateTime.Now.AddSeconds(duration);

        var statusTask = Task.Run(ShowStatus);

        var tasks = new List<Task>();
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() => SendRequests(serverUrl, CancellationTokenSource.Token)));
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nCritical error occurred: {ex.Message}");
        }
        finally
        {
            CancellationTokenSource.Cancel();
            await statusTask;
            
            Console.WriteLine("\n--- Test Summary ---");
            Console.WriteLine($"Total requests: {TotalRequests}");
            Console.WriteLine($"Average RPS: {TotalRequests / duration}");
            
            if (UniqueErrors.Count > 0)
            {
                Console.WriteLine("\nEncountered errors:");
                foreach (var error in UniqueErrors)
                {
                    Console.WriteLine($"- {error}");
                }
            }
        }
    }

    static async Task SendRequests(string serverUrl, CancellationToken token)
    {
        var options = new RestClientOptions
        {
            BaseUrl = new Uri(serverUrl),
            MaxTimeout = 100,
            RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true,
            FollowRedirects = true,
            MaxRedirects = 3
        };

        var client = new RestClient(options);

        while (DateTime.Now < EndTime && !token.IsCancellationRequested)
        {
            try
            {
                var request = new RestRequest("", Method.Post);
                request.AddFile("file", ImageBytes, "image.jpg");

                var response = await client.ExecuteAsync(request);
                
                if (response.IsSuccessful)
                {
                    Interlocked.Increment(ref TotalRequests);
                }
                else
                {
                    Interlocked.Increment(ref TotalRequests);
                    Interlocked.Increment(ref FailedRequests);
                    
                    var errorMessage = $"{response.StatusCode} - {response.ErrorMessage ?? "No error message"}";
                    lock (ErrorsLock)
                    {
                        UniqueErrors.Add(errorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref TotalRequests);
                Interlocked.Increment(ref FailedRequests);
                
                lock (ErrorsLock)
                {
                    UniqueErrors.Add(ex.Message);
                }
                
                await Task.Delay(100);
            }
        }
    }

    static async Task ShowStatus()
    {
        var stopwatch = Stopwatch.StartNew();
        long lastRequests = 0;
        long lastFailed = 0;

        while (DateTime.Now < EndTime && !CancellationTokenSource.Token.IsCancellationRequested)
        {
            await Task.Delay(1000);
            
            var currentRequests = Interlocked.Read(ref TotalRequests);
            var currentFailed = Interlocked.Read(ref FailedRequests);
            var rps = currentRequests - lastRequests;
            var memoryMB = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
            
            lock (ConsoleLock)
            {
                Console.Write($"\rTime: {stopwatch.Elapsed:mm\\:ss} | RPS: {rps} | Send: {currentRequests} | Memory: {memoryMB}MB   ");
            }
            
            lastRequests = currentRequests;
            lastFailed = currentFailed;
        }
    }
}