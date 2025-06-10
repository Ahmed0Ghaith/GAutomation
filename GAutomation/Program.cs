using Microsoft.Playwright;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private static readonly ConcurrentDictionary<int, bool> _activeInstances = new ConcurrentDictionary<int, bool>();
    private static volatile bool _isRunning = true;
    private static int noOfInstances = 1;
    private static int loopCount = 1;
    static string server = "";
    static string username = "";
    static string password = "";
    static string Selector1 = "";
    static string Selector2 = "";
    static string Selector3 = "";
    static string Selector4 = "";
    static string Selector5 = "";
    static int minMin = 3;
    static int maxMin = 5;





    public static async Task Main()
    {
        Console.WriteLine("Enter the Proxy Server URL with port like  eg https://geo.iproyal.com:12321");
        server = Console.ReadLine();
        if (string.IsNullOrEmpty(server))
        {
            Console.WriteLine("Enter a valid url");
            return;
        }

        if (!server.StartsWith("http"))
        {
            server = "https://" + server;
        }

        Console.WriteLine("Enter the Proxy Name :");
        username = Console.ReadLine();
        if (string.IsNullOrEmpty(username))
        {
            Console.WriteLine("Enter a valid User Name");
            return;
        }

        Console.WriteLine("Enter a valid User Password");
        password = Console.ReadLine();
        if (string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Enter a valid Password");
            return;
        }





        Console.WriteLine("Enter the website URL you want to visit:");
        string targetUrl = Console.ReadLine();
        Console.WriteLine("Enter the Min time by secound to wait eg 2 for 2 secound ");
       string min = Console.ReadLine();
        if (string.IsNullOrEmpty(min))
        {
            Console.WriteLine("Min time by secound to wait 5 s");

        }
        else
        {
            minMin = int.TryParse(min, out int minVal) ? minMin : 2;
        }
        Console.WriteLine("Enter the Max time by secound to wait eg 5 for 5 secound");
        string max = Console.ReadLine();
        if (string.IsNullOrEmpty(max))
        {
            Console.WriteLine("Max time by secound to wait 5 s");

        }
        else
        { 
        maxMin = int.TryParse(max, out int maxValue) ? maxValue : 5;
        }
        Console.WriteLine("Enter Frist Selector  to click ");
        Selector1 = Console.ReadLine();
        if (string.IsNullOrEmpty(Selector1))
        {
            Console.WriteLine("No XPath selector provided. Will only perform scrolling without clicking on Frist.");
        }
        else
        {
            Console.WriteLine("Enter Secound Selector  to click ");
            Selector2 = Console.ReadLine();
            if (string.IsNullOrEmpty(Selector2))
            {
                Console.WriteLine("No XPath selector provided. Will only perform scrolling without clicking on Secound.");
            }
            else
            {
                Console.WriteLine("Enter Third Selector  to click ");

                Selector3 = Console.ReadLine();
                if (string.IsNullOrEmpty(Selector3))
                {
                    Console.WriteLine("No XPath selector provided. Will only perform scrolling without clicking on Third.");
                }
                else
                {
                    Console.WriteLine("Enter Fourth Selector  to click ");

                    Selector4 = Console.ReadLine();
                    if (string.IsNullOrEmpty(Selector4))
                    {
                        Console.WriteLine("No XPath selector provided. Will only perform scrolling without clicking on Fourth.");
                    }
                    else
                    {
                        Console.WriteLine("Enter Fifth Selector  to click ");

                        Selector5 = Console.ReadLine();
                        if (string.IsNullOrEmpty(Selector5))
                        {
                            Console.WriteLine("No XPath selector provided. Will only perform scrolling without clicking on Fifth .");
                        }
                    }
                }
            }
        }
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
            loopCount = 1;
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

            // Keep the browser open for a random duration between 5-10 seconds after behavior simulation
            var random = new Random();
            await Task.Delay(random.Next(5000, 10000));

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
            var startTime = DateTime.Now;

            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting initial scroll behavior simulation");

            // First round of scrolling
            await PerformScrollingBehavior(page, instanceId, iteration, "Initial");

            // Try to click on the CSS selector
            if (!string.IsNullOrEmpty(Selector1))
            {
                await AttemptSelectorClick(Selector1, page, instanceId, iteration);
                if (!string.IsNullOrEmpty(Selector2))
                {
                    await AttemptSelectorClick(Selector2, page, instanceId, iteration);
                    if (!string.IsNullOrEmpty(Selector3))
                    { 
                    await AttemptSelectorClick(Selector3, page, instanceId, iteration);
                        if (!string.IsNullOrEmpty(Selector4))
                        { 
                    await AttemptSelectorClick(Selector4, page, instanceId, iteration);
                            if (!string.IsNullOrEmpty(Selector5))
                            {
                                await AttemptSelectorClick(Selector5, page, instanceId, iteration);

                            }
                        }
                    }
                }
            }
            // Second round of scrolling after clicking
       

            var elapsedTime = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"Instance {instanceId}-{iteration}: Completed full behavior simulation in {elapsedTime:F1} seconds");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Behavior simulation error: {ex.Message}");
        }
    }

    private static async Task PerformScrollingBehavior(IPage page, int instanceId, int iteration, string phase)
    {
        try
        {
            var random = new Random();
            var startTime = DateTime.Now;

            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting {phase} scroll behavior simulation");
            await Task.Delay(random.Next(minMin*1000, maxMin*1000));

            // Phase 1: Scroll down gradually (20 seconds)
            var scrollDownEndTime = startTime.AddSeconds(20);
            var totalScrollDown = 0;

            while (DateTime.Now < scrollDownEndTime)
            {
                // Random scroll down amount between 200-500 pixels
                var scrollAmount = random.Next(200, 500);

                // Use smooth scrolling
                await page.EvaluateAsync($"window.scrollBy({{top: {scrollAmount}, behavior: 'smooth'}})");
                totalScrollDown += scrollAmount;

                // Random delay between scrolls (800ms to 2 seconds for smoother experience)
                await Task.Delay(random.Next(800, 2000));

                Console.WriteLine($"Instance {instanceId}-{iteration}: {phase} - Scrolled down {scrollAmount}px (total: {totalScrollDown}px)");
            }

            // Small pause between scrolling phases
            await Task.Delay(random.Next(1000, 2000));

            // Phase 2: Scroll back to top (20 seconds)
            var scrollUpEndTime = startTime.AddSeconds(40);
            var totalScrollUp = 0;

            while (DateTime.Now < scrollUpEndTime)
            {
                // Random scroll up amount between 200-500 pixels
                var scrollAmount = random.Next(200, 500);

                // Use smooth scrolling upward
                await page.EvaluateAsync($"window.scrollBy({{top: -{scrollAmount}, behavior: 'smooth'}})");
                totalScrollUp += scrollAmount;

                // Random delay between scrolls
                await Task.Delay(random.Next(800, 2000));

                Console.WriteLine($"Instance {instanceId}-{iteration}: {phase} - Scrolled up {scrollAmount}px (total up: {totalScrollUp}px)");
            }

            // Ensure we're back at the top with smooth scrolling
            await page.EvaluateAsync("window.scrollTo({top: 0, behavior: 'smooth'})");
            await Task.Delay(1000);

            // Add some final mouse movements to simulate reading at the top
            for (int i = 0; i < random.Next(2, 4); i++)
            {
                var x = random.Next(100, 700);
                var y = random.Next(100, 300);
                await page.Mouse.MoveAsync(x, y);
                await Task.Delay(random.Next(300, 800));
            }

            Console.WriteLine($"Instance {instanceId}-{iteration}: Completed {phase} scroll behavior");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: {phase} scroll behavior error: {ex.Message}");
        }
    }

    private static async Task AttemptSelectorClick(string Selector,IPage page, int instanceId, int iteration)
    {
        try
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Attempting to find and click CSS selector: {Selector}");

            var element = page.Locator(Selector);
            await element.ClickAsync();
            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting second round of scrolling");
            await PerformScrollingBehavior(page, instanceId, iteration, nameof(Selector));
        }
        catch (TimeoutException)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Timeout waiting for CSS selector '#post-22 > div > div > header > h4 > a' - element may not exist");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Error clicking CSS selector '#post-22 > div > div > header > h4 > a': {ex.Message}");
        }
    }
}