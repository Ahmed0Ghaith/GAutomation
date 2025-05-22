using Microsoft.Playwright;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(5); // Limit to 5 concurrent instances
    private static readonly ConcurrentDictionary<int, bool> _activeInstances = new ConcurrentDictionary<int, bool>();
    private static volatile bool _isRunning = true;
    private static int noOfInstances = 5;

    public static async Task Main()
    {
        Console.WriteLine("Enter the website URL you want to visit:");
        string targetUrl = Console.ReadLine();
        Console.WriteLine("Enter the number of iterations:");

        if (!int.TryParse(Console.ReadLine(), out int loopCount))
        {
            Console.WriteLine("Invalid input. Using default count of 10.");
            loopCount = 10;
        }
        Console.WriteLine("Enter the number of Instances:");

        if (!int.TryParse(Console.ReadLine(), out int noOfInstances))
        {
            Console.WriteLine("Invalid input. Using default count of 5.");
            noOfInstances = 5;
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

        Console.WriteLine("Starting browser instances with unique fingerprints...");

        // Start monitoring task
        var monitorTask = MonitorAndReplaceInstances(targetUrl);

        // Start main processing
        var processingTask = ProcessInstances(targetUrl, loopCount);

        // Wait for user input to stop
        Console.WriteLine("Press 'Q' to quit...");
        while (Console.ReadKey(true).Key != ConsoleKey.Q) { }

        _isRunning = false;
        await Task.WhenAll(monitorTask, processingTask);
    }

    private static async Task ProcessInstances(string targetUrl, int loopCount)
    {
        var tasks = new List<Task>();
        for (int i = 0; i < loopCount; i++)
        {
            if (!_isRunning) break;

            await _semaphore.WaitAsync();
            var instanceId = i;
            _activeInstances[instanceId] = true;

            var task = RunBrowserInstance(targetUrl, instanceId);
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);
    }

    private static async Task MonitorAndReplaceInstances(string targetUrl)
    {
        while (_isRunning)
        {
            if (_activeInstances.Count < noOfInstances)
            {
                var newInstanceId = _activeInstances.Count;
                if (_semaphore.CurrentCount > 0 && !_activeInstances.ContainsKey(newInstanceId))
                {
                    await _semaphore.WaitAsync();
                    _activeInstances[newInstanceId] = true;
                    _ = RunBrowserInstance(targetUrl, newInstanceId);
                }
            }
            await Task.Delay(1000); // Check every second
        }
    }

    private static async Task RunBrowserInstance(string targetUrl, int instanceId)
    {
        try
        {
            // Generate unique fingerprint for this instance
            var fingerprintProfile = BrowserFingerprintManager.GenerateRandomFingerprint(instanceId);

            Console.WriteLine($"Instance {instanceId}: Generated fingerprint - UA: {fingerprintProfile.UserAgent.Substring(0, 50)}...");
            Console.WriteLine($"Instance {instanceId}: Platform: {fingerprintProfile.Platform}, Viewport: {fingerprintProfile.ViewportSize.Width}x{fingerprintProfile.ViewportSize.Height}");

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
                Server = "https://geo.iproyal.com:12321",
                Username = "kx25pY99mFbmORvY",
                Password = "MhTTC3Ajh8bHkk8B_country-gb"
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

            Console.WriteLine($"Instance {instanceId}: Starting navigation to {targetUrl}");
            await page.GotoAsync(targetUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

            // Simulate human behavior
            await SimulateHumanBehavior(page, instanceId);

            // Keep the browser open for a random duration between 10-60 seconds
            var random = new Random();
            await Task.Delay(random.Next(10000, 60000));

            await browser.CloseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId} error: {ex.Message}");
        }
        finally
        {
            _activeInstances.TryRemove(instanceId, out _);
            _semaphore.Release();
            Console.WriteLine($"Instance {instanceId}: Closed");
        }
    }

    private static async Task SimulateHumanBehavior(IPage page, int instanceId)
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

            Console.WriteLine($"Instance {instanceId}: Completed human behavior simulation");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}: Behavior simulation error: {ex.Message}");
        }
    }
}