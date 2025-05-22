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
    private static int loopCount = 1;
    static string server = "https://geo.iproyal.com:12321";
    static string username = "kx25pY99mFbmORvY";
    static string password = "MhTTC3Ajh8bHkk8B_country-il";
    static string cssSelector = "";

    public static async Task Main()
    {
        Console.WriteLine("Enter the Proxy Server URL with port like  eg https://geo.iproyal.com:12321");
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

        //Console.WriteLine("Enter a valid User Password");
        //password = Console.ReadLine();
        //if (string.IsNullOrEmpty(password))
        //{
        //    Console.WriteLine("Enter a valid Password");
        //    return;
        //}

    
        


        Console.WriteLine("Enter the website URL you want to visit:");
        string targetUrl = Console.ReadLine();

        Console.WriteLine("Enter Selector  to click ");
        cssSelector = Console.ReadLine();
        if (string.IsNullOrEmpty(cssSelector))
        {
            Console.WriteLine("No XPath selector provided. Will only perform scrolling without clicking.");
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

        Console.WriteLine($"Starting {noOfInstances} browser instances, each running {loopCount} iterations...");
        if (!string.IsNullOrEmpty(cssSelector))
        {
            Console.WriteLine($"Will click on elements matching XPath: {cssSelector}");
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
            await AttemptSelectorClick(page, instanceId, iteration);

            // Second round of scrolling after clicking
            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting second round of scrolling");
            await PerformScrollingBehavior(page, instanceId, iteration, "Second");

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

    private static async Task AttemptSelectorClick(IPage page, int instanceId, int iteration)
    {
        try
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Attempting to find and click CSS selector: {cssSelector}");

            var element = await page.QuerySelectorAsync(cssSelector);
            await element.ClickAsync();
            //if (element != null)
            //{
            //    await element.ClickAsync();
            //    Console.WriteLine($"Instance {instanceId}-{iteration}: Successfully clicked on CSS selector element (simple click)");
            //    await Task.Delay(new Random().Next(1000, 2000));

            //}
            //else
            //{ 
            //    Console.WriteLine($"Instance {instanceId}-{iteration}: Element with CSS selector '{cssSelector}' not found or not visible");

            //}
            //// Wait for the element to be available using CSS selector
            //var element = await page.WaitForSelectorAsync(cssSelector, new PageWaitForSelectorOptions
            //{
            //    Timeout = 10000,
            //    State = WaitForSelectorState.Visible
            //});

            //if (element != null)
            //{
            //    // Scroll element into view smoothly
            //    await element.ScrollIntoViewIfNeededAsync();
            //    await Task.Delay(500);

            //    // Get element position for more natural clicking
            //    var boundingBox = await element.BoundingBoxAsync();
            //    if (boundingBox != null)
            //    {
            //        var random = new Random();
            //        var clickX = (float)(boundingBox.X + (boundingBox.Width * random.NextDouble()));
            //        var clickY = (float)(boundingBox.Y + (boundingBox.Height * random.NextDouble()));

            //        // Move mouse to the element first, then click
            //        await page.Mouse.MoveAsync(clickX, clickY);

            //        await Task.Delay(random.Next(200, 500));

            //        await element.ClickAsync();
            //        Console.WriteLine($"Instance {instanceId}-{iteration}: Successfully clicked on CSS selector element at ({clickX:F1}, {clickY:F1})");

            //        // Wait a bit after clicking
            //        await Task.Delay(random.Next(1000, 2000));
            //    }
            //    else
            //    {
            //        // Fallback to simple click
            //        await element.ClickAsync();
            //        Console.WriteLine($"Instance {instanceId}-{iteration}: Successfully clicked on CSS selector element (simple click)");
            //        await Task.Delay(new Random().Next(1000, 2000));
            //    }
            //}
            //else
            //{
            //    Console.WriteLine($"Instance {instanceId}-{iteration}: Element with CSS selector '{cssSelector}' not found or not visible");
            //}
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