using Microsoft.Playwright;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
    private static readonly ConcurrentDictionary<int, bool> _activeInstances = new ConcurrentDictionary<int, bool>();
    private static volatile bool _isRunning = true;
    private static int noOfInstances = 1;
    private static int loopCount = 10;
    static string server = "https://geo.iproyal.com:12321";
    static string username = "kx25pY99mFbmORvY";
    static string password = "MhTTC3Ajh8bHkk8B_country-il";

    public static async Task Main()
    {
        //Console.WriteLine("Enter the Proxy Server URL with port like  eg https://geo.iproyal.com:12321");
        //server = Console.ReadLine();
        //if (string.IsNullOrEmpty(server))
        //{
        //    Console.WriteLine("Enter a valid url");
        //    return;
        //}

        //if (!server.StartsWith("http"))
        //{
        //    server = "https://" + server;
        //}

        //Console.WriteLine("Enter the Proxy Name :");
        //username = Console.ReadLine();
        //if (string.IsNullOrEmpty(username))
        //{
        //    Console.WriteLine("Enter a valid User Name");
        //    return;
        //}

        //Console.WriteLine("Enter the Proxy Password");
        //password = Console.ReadLine();
        //if (string.IsNullOrEmpty(password))
        //{
        //    Console.WriteLine("Enter a valid Password");
        //    return;
        //}

        Console.WriteLine("Enter the website URL you want to visit:");
        string targetUrl = Console.ReadLine();

        Console.WriteLine("Enter the number of browser instances to open:");
        if (!int.TryParse(Console.ReadLine(), out noOfInstances))
        {
            Console.WriteLine("Invalid input. Using default count of 5.");
            noOfInstances = 5;
        }

        Console.WriteLine("Enter the number of iterations per instance:");
        if (!int.TryParse(Console.ReadLine(), out loopCount))
        {
            Console.WriteLine("Invalid input. Using default count of 10.");
            loopCount = 10;
        }

        if (string.IsNullOrEmpty(targetUrl))
        {
            Console.WriteLine("No URL provided. Using default website.");
            targetUrl = "https://www.google.com";
        }

        if (!targetUrl.StartsWith("http"))
        {
            targetUrl = "https://" + targetUrl;
        }

        Console.WriteLine($"Starting {noOfInstances} browser instances, each running {loopCount} iterations...");

        // Start the main processing with correct logic
        var processingTask = ProcessInstances(targetUrl);

        // Wait for user input to stop
        Console.WriteLine("Press 'Q' to quit...");
        while (Console.ReadKey(true).Key != ConsoleKey.Q) { }

        _isRunning = false;
        await processingTask;
    }

    private static async Task ProcessInstances(string targetUrl)
    {
        var instanceTasks = new List<Task>();

        // Create the specified number of browser instances
        for (int instanceId = 0; instanceId < noOfInstances; instanceId++)
        {
            if (!_isRunning) break;

            _activeInstances[instanceId] = true;

            // Each instance will run its own iterations
            var task = RunBrowserInstanceWithIterations(targetUrl, instanceId);
            instanceTasks.Add(task);
        }

        // Wait for all instances to complete
        await Task.WhenAll(instanceTasks);
    }

    private static async Task RunBrowserInstanceWithIterations(string targetUrl, int instanceId)
    {
        Console.WriteLine($"Instance {instanceId}: Started with {loopCount} iterations");

        for (int iteration = 0; iteration < loopCount && _isRunning; iteration++)
        {
            try
            {
                Console.WriteLine($"Instance {instanceId}: Starting iteration {iteration + 1}/{loopCount}");
                await RunSingleBrowserSession(targetUrl, instanceId, iteration);

                // Small delay between iterations for the same instance
                if (iteration < loopCount - 1 && _isRunning)
                {
                    var random = new Random();
                    await Task.Delay(random.Next(2000, 5000));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Instance {instanceId}, Iteration {iteration + 1}: Error - {ex.Message}");
            }
        }

        _activeInstances.TryRemove(instanceId, out _);
        Console.WriteLine($"Instance {instanceId}: Completed all iterations");
    }

    private static async Task RunSingleBrowserSession(string targetUrl, int instanceId, int iteration)
    {
        try
        {
            // Generate unique fingerprint for this session
            var fingerprintProfile = BrowserFingerprintManager.GenerateRandomFingerprint(instanceId * 1000 + iteration);

            Console.WriteLine($"Instance {instanceId}-{iteration}: Generated fingerprint - UA: {fingerprintProfile.UserAgent.Substring(0, 50)}...");
            Console.WriteLine($"Instance {instanceId}-{iteration}: Platform: {fingerprintProfile.Platform}, Viewport: {fingerprintProfile.ViewportSize.Width}x{fingerprintProfile.ViewportSize.Height}");

            using var playwright = await Playwright.CreateAsync();

            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 50,
                Args = fingerprintProfile.BrowserArgs
            };

            await using var browser = await playwright.Chromium.LaunchAsync(launchOptions);

            // Create proxy configuration
            var proxy = new Proxy
            {
                Server = server,
                Username = username,
                Password = password,
            };

            // Create context with fingerprint profile
            var contextOptions = BrowserFingerprintManager.CreateContextOptions(fingerprintProfile, proxy);
            var context = await browser.NewContextAsync(contextOptions);

            // Apply additional fingerprint modifications
            await BrowserFingerprintManager.ApplyFingerprintToContext(context, fingerprintProfile);

            var page = await context.NewPageAsync();

            // Add additional stealth measures
            await page.AddInitScriptAsync(@"
                // Remove webdriver property
                delete navigator.__proto__.webdriver;
                
                // Mock permissions
                const originalQuery = window.navigator.permissions.query;
                window.navigator.permissions.query = (parameters) => (
                    parameters.name === 'notifications' ?
                        Promise.resolve({ state: Notification.permission }) :
                        originalQuery(parameters)
                );

                // Mock plugins
                Object.defineProperty(navigator, 'plugins', {
                    get: () => [
                        {
                            0: { type: 'application/x-google-chrome-pdf', suffixes: 'pdf', description: 'Portable Document Format', enabledPlugin: Plugin },
                            description: 'Portable Document Format',
                            filename: 'internal-pdf-viewer',
                            length: 1,
                            name: 'Chrome PDF Plugin'
                        }
                    ]
                });
            ");

            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting navigation to {targetUrl}");
            await page.GotoAsync(targetUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

            // Simulate human behavior
            await SimulateHumanBehavior(page, instanceId, iteration);

            // Keep the browser open for a random duration between 10-30 seconds
            var random = new Random();
            await Task.Delay(random.Next(10000, 30000));

            await browser.CloseAsync();
            Console.WriteLine($"Instance {instanceId}-{iteration}: Session completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration} error: {ex.Message}");
        }
    }

    private static async Task SimulateHumanBehavior(IPage page, int instanceId, int iteration)
    {
        try
        {
            var random = new Random();

            // Random mouse movements
            for (int i = 0; i < random.Next(2, 5); i++)
            {
                var x = random.Next(100, 700);
                var y = random.Next(100, 500);
                await page.Mouse.MoveAsync(x, y);
                await Task.Delay(random.Next(500, 1500));
            }

            // Random scrolling
            for (int i = 0; i < random.Next(1, 3); i++)
            {
                await page.Mouse.WheelAsync(0, random.Next(100, 500));
                await Task.Delay(random.Next(1000, 2000));
            }

            // Try to interact with common elements if they exist
            var commonSelectors = new[] { "input[type='text']", "input[type='search']", "a", "button" };

            foreach (var selector in commonSelectors)
            {
                try
                {
                    var elements = await page.QuerySelectorAllAsync(selector);
                    if (elements.Count > 0 && random.Next(3) == 0) // 33% chance to interact
                    {
                        var element = elements[random.Next(elements.Count)];
                        await element.HoverAsync();
                        await Task.Delay(random.Next(500, 1000));
                        break;
                    }
                }
                catch
                {
                    // Ignore errors for optional interactions
                }
            }

            Console.WriteLine($"Instance {instanceId}-{iteration}: Completed human behavior simulation");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Behavior simulation error: {ex.Message}");
        }
    }
}