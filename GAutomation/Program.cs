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
    static List<ActionItem> actionSequence = new List<ActionItem>();

    public class ActionItem
    {
        public string Type { get; set; } // "wait", "scroll_down", "scroll_up", "click"
        public int Duration { get; set; } // For wait and scroll actions (in seconds)
        public string Selector { get; set; } // For click actions
        public string Description { get; set; }
    }

    public static async Task Main()
    {
        Console.WriteLine("=== Playwright Browser Automation Tool ===\n");

        // Get proxy configuration
        if (!GetProxyConfiguration()) return;

        // Get target URL
        Console.WriteLine("Enter the website URL you want to visit:");
        string targetUrl = Console.ReadLine();
        if (string.IsNullOrEmpty(targetUrl))
        {
            Console.WriteLine("No URL provided. Using default website.");
            targetUrl = "https://www.google.com";
        }
        if (!targetUrl.StartsWith("http"))
        {
            targetUrl = "https://" + targetUrl;
        }

        // Get action sequence
        GetActionSequence();

        // Get instance configuration
        GetInstanceConfiguration();

        Console.WriteLine("\n=== Configuration Summary ===");
        Console.WriteLine($"Target URL: {targetUrl}");
        Console.WriteLine($"Browser Instances: {noOfInstances}");
        Console.WriteLine($"Iterations per Instance: {loopCount}");
        Console.WriteLine($"Total Actions per Session: {actionSequence.Count}");
        Console.WriteLine("\nAction Sequence:");
        for (int i = 0; i < actionSequence.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {actionSequence[i].Description}");
        }

        Console.WriteLine("\nStarting automation...");

        // Start the main processing
        var processingTask = ProcessInstances(targetUrl);

        // Wait for user input to stop
        Console.WriteLine("\nPress 'Q' to quit...");
        while (Console.ReadKey(true).Key != ConsoleKey.Q) { }

        _isRunning = false;
        await processingTask;
    }

    private static bool GetProxyConfiguration()
    {
        Console.WriteLine("Enter the Proxy Server URL with port (e.g., https://geo.iproyal.com:12321):");
        server = Console.ReadLine();
        if (string.IsNullOrEmpty(server))
        {
            Console.WriteLine("Enter a valid URL");
            return false;
        }
        if (!server.StartsWith("http"))
        {
            server = "https://" + server;
        }

        Console.WriteLine("Enter the Proxy Username:");
        username = Console.ReadLine();
        if (string.IsNullOrEmpty(username))
        {
            Console.WriteLine("Enter a valid Username");
            return false;
        }

        Console.WriteLine("Enter the Proxy Password:");
        password = Console.ReadLine();
        if (string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Enter a valid Password");
            return false;
        }

        return true;
    }

    private static void GetActionSequence()
    {
        Console.WriteLine("\n=== Action Sequence Configuration ===");
        Console.WriteLine("Enter your action sequence. Available actions:");
        Console.WriteLine("1. wait [seconds] - Wait for specified seconds");
        Console.WriteLine("2. scroll_down [seconds] - Scroll down for specified seconds");
        Console.WriteLine("3. scroll_up [seconds] - Scroll up for specified seconds");
        Console.WriteLine("4. click [selector] - Click on CSS/XPath selector");
        Console.WriteLine("5. done - Finish entering actions");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  wait 20");
        Console.WriteLine("  scroll_down 15");
        Console.WriteLine("  click #submit-button");
        Console.WriteLine("  click //button[@class='login-btn']");

        int actionNumber = 1;
        while (true)
        {
            Console.Write($"\nAction {actionNumber}: ");
            string input = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("Please enter an action or 'done' to finish.");
                continue;
            }

            if (input == "done")
            {
                break;
            }

            var parts = input.Split(' ', 2);
            if (parts.Length < 1)
            {
                Console.WriteLine("Invalid format. Please try again.");
                continue;
            }

            string actionType = parts[0];
            ActionItem action = new ActionItem();

            switch (actionType)
            {
                case "wait":
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int waitSeconds))
                    {
                        Console.WriteLine("Invalid wait format. Use: wait [seconds]");
                        continue;
                    }
                    action.Type = "wait";
                    action.Duration = waitSeconds;
                    action.Description = $"Wait for {waitSeconds} seconds";
                    break;

                case "scroll_down":
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int scrollDownSeconds))
                    {
                        Console.WriteLine("Invalid scroll_down format. Use: scroll_down [seconds]");
                        continue;
                    }
                    action.Type = "scroll_down";
                    action.Duration = scrollDownSeconds;
                    action.Description = $"Scroll down for {scrollDownSeconds} seconds";
                    break;

                case "scroll_up":
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int scrollUpSeconds))
                    {
                        Console.WriteLine("Invalid scroll_up format. Use: scroll_up [seconds]");
                        continue;
                    }
                    action.Type = "scroll_up";
                    action.Duration = scrollUpSeconds;
                    action.Description = $"Scroll up for {scrollUpSeconds} seconds";
                    break;

                case "click":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Invalid click format. Use: click [selector]");
                        continue;
                    }
                    action.Type = "click";
                    action.Selector = parts[1];
                    action.Description = $"Click on selector: {parts[1]}";
                    break;

                default:
                    Console.WriteLine("Unknown action. Available actions: wait, scroll_down, scroll_up, click, done");
                    continue;
            }

            actionSequence.Add(action);
            Console.WriteLine($"Added: {action.Description}");
            actionNumber++;
        }

        if (actionSequence.Count == 0)
        {
            Console.WriteLine("No actions specified. Adding default sequence...");
            actionSequence.Add(new ActionItem { Type = "wait", Duration = 5, Description = "Wait for 5 seconds" });
            actionSequence.Add(new ActionItem { Type = "scroll_down", Duration = 20, Description = "Scroll down for 20 seconds" });
            actionSequence.Add(new ActionItem { Type = "scroll_up", Duration = 20, Description = "Scroll up for 20 seconds" });
        }
    }

    private static void GetInstanceConfiguration()
    {
        Console.WriteLine("\nEnter the number of browser instances to open:");
        if (!int.TryParse(Console.ReadLine(), out noOfInstances) || noOfInstances <= 0)
        {
            Console.WriteLine("Invalid input. Using default count of 1.");
            noOfInstances = 1;
        }

        Console.WriteLine("Enter the number of iterations per instance:");
        if (!int.TryParse(Console.ReadLine(), out loopCount) || loopCount <= 0)
        {
            Console.WriteLine("Invalid input. Using default count of 1.");
            loopCount = 1;
        }
    }

    private static async Task ProcessInstances(string targetUrl)
    {
        var instanceTasks = new List<Task>();

        for (int instanceId = 0; instanceId < noOfInstances; instanceId++)
        {
            if (!_isRunning) break;

            _activeInstances[instanceId] = true;
            var task = RunBrowserInstanceWithIterations(targetUrl, instanceId);
            instanceTasks.Add(task);
        }

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
            var fingerprintProfile = BrowserFingerprintManager.GenerateRandomFingerprint(instanceId * 1000 + iteration);

            Console.WriteLine($"Instance {instanceId}-{iteration}: Generated fingerprint - UA: {fingerprintProfile.UserAgent.Substring(0, Math.Min(50, fingerprintProfile.UserAgent.Length))}...");
            Console.WriteLine($"Instance {instanceId}-{iteration}: Platform: {fingerprintProfile.Platform}, Viewport: {fingerprintProfile.ViewportSize.Width}x{fingerprintProfile.ViewportSize.Height}");

            using var playwright = await Playwright.CreateAsync();

            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 50,
                Args = fingerprintProfile.BrowserArgs
            };

            await using var browser = await playwright.Chromium.LaunchAsync(launchOptions);

            var proxy = new Proxy
            {
                Server = server,
                Username = username,
                Password = password,
            };

            var contextOptions = BrowserFingerprintManager.CreateContextOptions(fingerprintProfile, proxy);
            var context = await browser.NewContextAsync(contextOptions);

            await BrowserFingerprintManager.ApplyFingerprintToContext(context, fingerprintProfile);

            var page = await context.NewPageAsync();

            // Add stealth measures (removed PDF plugin mock)
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
            ");

            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting navigation to {targetUrl}");
            await page.GotoAsync(targetUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

            // Execute the action sequence
            await ExecuteActionSequence(page, instanceId, iteration);

            // Keep browser open for a short time after completion
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

    private static async Task ExecuteActionSequence(IPage page, int instanceId, int iteration)
    {
        try
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting action sequence ({actionSequence.Count} actions)");

            for (int i = 0; i < actionSequence.Count; i++)
            {
                if (!_isRunning) break;

                var action = actionSequence[i];
                Console.WriteLine($"Instance {instanceId}-{iteration}: Executing action {i + 1}/{actionSequence.Count} - {action.Description}");

                switch (action.Type)
                {
                    case "wait":
                        await Task.Delay(action.Duration * 1000);
                        break;

                    case "scroll_down":
                        await PerformScrollDown(page, action.Duration, instanceId, iteration);
                        break;

                    case "scroll_up":
                        await PerformScrollUp(page, action.Duration, instanceId, iteration);
                        break;

                    case "click":
                        await AttemptSelectorClick(action.Selector, page, instanceId, iteration);
                        break;

                    default:
                        Console.WriteLine($"Instance {instanceId}-{iteration}: Unknown action type: {action.Type}");
                        break;
                }

                // Small delay between actions
                if (i < actionSequence.Count - 1)
                {
                    await Task.Delay(new Random().Next(500, 1500));
                }
            }

            Console.WriteLine($"Instance {instanceId}-{iteration}: Completed all actions");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Action sequence error: {ex.Message}");
        }
    }

    private static async Task PerformScrollDown(IPage page, int durationSeconds, int instanceId, int iteration)
    {
        try
        {
            var random = new Random();
            var startTime = DateTime.Now;
            var endTime = startTime.AddSeconds(durationSeconds);
            var totalScrolled = 0;

            while (DateTime.Now < endTime && _isRunning)
            {
                var scrollAmount = random.Next(200, 500);
                await page.EvaluateAsync($"window.scrollBy({{top: {scrollAmount}, behavior: 'smooth'}})");
                totalScrolled += scrollAmount;

                await Task.Delay(random.Next(800, 2000));
            }

            Console.WriteLine($"Instance {instanceId}-{iteration}: Scrolled down {totalScrolled}px in {durationSeconds} seconds");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Scroll down error: {ex.Message}");
        }
    }

    private static async Task PerformScrollUp(IPage page, int durationSeconds, int instanceId, int iteration)
    {
        try
        {
            var random = new Random();
            var startTime = DateTime.Now;
            var endTime = startTime.AddSeconds(durationSeconds);
            var totalScrolled = 0;

            while (DateTime.Now < endTime && _isRunning)
            {
                var scrollAmount = random.Next(200, 500);
                await page.EvaluateAsync($"window.scrollBy({{top: -{scrollAmount}, behavior: 'smooth'}})");
                totalScrolled += scrollAmount;

                await Task.Delay(random.Next(800, 2000));
            }

            Console.WriteLine($"Instance {instanceId}-{iteration}: Scrolled up {totalScrolled}px in {durationSeconds} seconds");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Scroll up error: {ex.Message}");
        }
    }

    private static async Task AttemptSelectorClick(string selector, IPage page, int instanceId, int iteration)
    {
        try
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Attempting to click selector: {selector}");

            var element = page.Locator(selector);
            await element.ClickAsync(new LocatorClickOptions { Timeout = 10000 });

            Console.WriteLine($"Instance {instanceId}-{iteration}: Successfully clicked selector: {selector}");

            // Add random mouse movement after click
            var random = new Random();
            await page.Mouse.MoveAsync(random.Next(100, 800), random.Next(100, 600));
            await Task.Delay(random.Next(500, 1500));
        }
        catch (TimeoutException)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Timeout - selector '{selector}' not found or not clickable");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Error clicking selector '{selector}': {ex.Message}");
        }
    }
}

//class Program
//{
//    private static readonly ConcurrentDictionary<int, bool> _activeInstances = new ConcurrentDictionary<int, bool>();
//    private static volatile bool _isRunning = true;
//    private static int noOfInstances = 1;
//    private static int loopCount = 1;
//    static string server = "";
//    static string username = "";
//    static string password = "";
//    static string Selector1 = "";
//    static string Selector2 = "";
//    static string Selector3 = "";
//    static string Selector4 = "";
//    static string Selector5 = "";
//    static int minMin = 3;
//    static int maxMin = 5;
//    static int scrollTime = 20;





//    public static async Task Main()
//    {
//        Console.WriteLine("Enter the Proxy Server URL with port like  eg https://geo.iproyal.com:12321");
//        server = Console.ReadLine();
//        if (string.IsNullOrEmpty(server))
//        {
//            Console.WriteLine("Enter a valid url");
//            return;
//        }

//        if (!server.StartsWith("http"))
//        {
//            server = "https://" + server;
//        }

//        Console.WriteLine("Enter the Proxy Name :");
//        username = Console.ReadLine();
//        if (string.IsNullOrEmpty(username))
//        {
//            Console.WriteLine("Enter a valid User Name");
//            return;
//        }

//        Console.WriteLine("Enter a valid User Password");
//        password = Console.ReadLine();
//        if (string.IsNullOrEmpty(password))
//        {
//            Console.WriteLine("Enter a valid Password");
//            return;
//        }





//        Console.WriteLine("Enter the website URL you want to visit:");
//        string targetUrl = Console.ReadLine();
//        Console.WriteLine("Enter the Min time by secound to wait eg 2 for 2 secound ");
//       string min = Console.ReadLine();
//        if (string.IsNullOrEmpty(min))
//        {
//            Console.WriteLine("Min time by secound to wait 5 s");

//        }
//        else
//        {
//            minMin = int.TryParse(min, out int minVal) ? minMin : 2;
//        }
//        Console.WriteLine("Enter the Max time by secound to wait eg 5 for 5 secound");
//        string max = Console.ReadLine();
//        if (string.IsNullOrEmpty(max))
//        {
//            Console.WriteLine("Max time by secound to wait 5 s");

//        }
//        else
//        { 
//        maxMin = int.TryParse(max, out int maxValue) ? maxValue : 5;
//        }


//        Console.WriteLine("Enter time to scroll by secound eg 1 for 1s ");
//        string scrollT = Console.ReadLine();
//        if (string.IsNullOrEmpty(scrollT))
//        {
//            Console.WriteLine("Default time will be 20 S ");

//        }
//        else
//        {
//            scrollTime = int.TryParse(scrollT, out int scrolltime) ? scrollTime : 20;
//        }
//        Console.WriteLine("Enter Frist Selector  to click ");
//        Selector1 = Console.ReadLine();
//        if (string.IsNullOrEmpty(Selector1))
//        {
//            Console.WriteLine("No XPath selector provided. Will only perform scrolling without clicking on Frist.");
//        }
//        else
//        {
//            Console.WriteLine("Enter Secound Selector  to click ");
//            Selector2 = Console.ReadLine();
//            if (string.IsNullOrEmpty(Selector2))
//            {
//                Console.WriteLine("No XPath selector provided. Will only perform scrolling without clicking on Secound.");
//            }
//            else
//            {
//                Console.WriteLine("Enter Third Selector  to click ");

//                Selector3 = Console.ReadLine();
//                if (string.IsNullOrEmpty(Selector3))
//                {
//                    Console.WriteLine("No XPath selector provided. Will only perform scrolling without clicking on Third.");
//                }
//                else
//                {
//                    Console.WriteLine("Enter Fourth Selector  to click ");

//                    Selector4 = Console.ReadLine();
//                    if (string.IsNullOrEmpty(Selector4))
//                    {
//                        Console.WriteLine("No XPath selector provided. Will only perform scrolling without clicking on Fourth.");
//                    }
//                    else
//                    {
//                        Console.WriteLine("Enter Fifth Selector  to click ");

//                        Selector5 = Console.ReadLine();
//                        if (string.IsNullOrEmpty(Selector5))
//                        {
//                            Console.WriteLine("No XPath selector provided. Will only perform scrolling without clicking on Fifth .");
//                        }
//                    }
//                }
//            }
//        }
//            Console.WriteLine("Enter the number of browser instances to open:");

//        if (!int.TryParse(Console.ReadLine(), out noOfInstances))
//        {
//            Console.WriteLine("Invalid input. Using default count of 5.");
//            noOfInstances = 5;
//        }

//        Console.WriteLine("Enter the number of iterations per instance:");
//        if (!int.TryParse(Console.ReadLine(), out loopCount))
//        {
//            Console.WriteLine("Invalid input. Using default count of 10.");
//            loopCount = 1;
//        }

//        if (string.IsNullOrEmpty(targetUrl))
//        {
//            Console.WriteLine("No URL provided. Using default website.");
//            targetUrl = "https://www.google.com";
//        }

//        if (!targetUrl.StartsWith("http"))
//        {
//            targetUrl = "https://" + targetUrl;
//        }


//        // Start the main processing with correct logic
//        var processingTask = ProcessInstances(targetUrl);

//        // Wait for user input to stop
//        Console.WriteLine("Press 'Q' to quit...");
//        while (Console.ReadKey(true).Key != ConsoleKey.Q) { }

//        _isRunning = false;
//        await processingTask;
//    }

//    private static async Task ProcessInstances(string targetUrl)
//    {
//        var instanceTasks = new List<Task>();

//        // Create the specified number of browser instances
//        for (int instanceId = 0; instanceId < noOfInstances; instanceId++)
//        {
//            if (!_isRunning) break;

//            _activeInstances[instanceId] = true;

//            // Each instance will run its own iterations
//            var task = RunBrowserInstanceWithIterations(targetUrl, instanceId);
//            instanceTasks.Add(task);
//        }

//        // Wait for all instances to complete
//        await Task.WhenAll(instanceTasks);
//    }

//    private static async Task RunBrowserInstanceWithIterations(string targetUrl, int instanceId)
//    {
//        Console.WriteLine($"Instance {instanceId}: Started with {loopCount} iterations");

//        for (int iteration = 0; iteration < loopCount && _isRunning; iteration++)
//        {
//            try
//            {
//                Console.WriteLine($"Instance {instanceId}: Starting iteration {iteration + 1}/{loopCount}");
//                await RunSingleBrowserSession(targetUrl, instanceId, iteration);

//                // Small delay between iterations for the same instance
//                if (iteration < loopCount - 1 && _isRunning)
//                {
//                    var random = new Random();
//                    await Task.Delay(random.Next(2000, 5000));
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Instance {instanceId}, Iteration {iteration + 1}: Error - {ex.Message}");
//            }
//        }

//        _activeInstances.TryRemove(instanceId, out _);
//        Console.WriteLine($"Instance {instanceId}: Completed all iterations");
//    }

//    private static async Task RunSingleBrowserSession(string targetUrl, int instanceId, int iteration)
//    {
//        try
//        {
//            // Generate unique fingerprint for this session
//            var fingerprintProfile = BrowserFingerprintManager.GenerateRandomFingerprint(instanceId * 1000 + iteration);

//            Console.WriteLine($"Instance {instanceId}-{iteration}: Generated fingerprint - UA: {fingerprintProfile.UserAgent.Substring(0, 50)}...");
//            Console.WriteLine($"Instance {instanceId}-{iteration}: Platform: {fingerprintProfile.Platform}, Viewport: {fingerprintProfile.ViewportSize.Width}x{fingerprintProfile.ViewportSize.Height}");

//            using var playwright = await Playwright.CreateAsync();

//            var launchOptions = new BrowserTypeLaunchOptions
//            {
//                Headless = false,
//                SlowMo = 50,
//                Args = fingerprintProfile.BrowserArgs
//            };

//            await using var browser = await playwright.Chromium.LaunchAsync(launchOptions);

//            // Create proxy configuration
//            var proxy = new Proxy
//            {
//                Server = server,
//                Username = username,
//                Password = password,
//            };

//            // Create context with fingerprint profile
//            var contextOptions = BrowserFingerprintManager.CreateContextOptions(fingerprintProfile, proxy);
//            var context = await browser.NewContextAsync(contextOptions);

//            // Apply additional fingerprint modifications
//            await BrowserFingerprintManager.ApplyFingerprintToContext(context, fingerprintProfile);

//            var page = await context.NewPageAsync();

//            // Add additional stealth measures
//            await page.AddInitScriptAsync(@"
//                // Remove webdriver property
//                delete navigator.__proto__.webdriver;

//                // Mock permissions
//                const originalQuery = window.navigator.permissions.query;
//                window.navigator.permissions.query = (parameters) => (
//                    parameters.name === 'notifications' ?
//                        Promise.resolve({ state: Notification.permission }) :
//                        originalQuery(parameters)
//                );

//                // Mock plugins
//                Object.defineProperty(navigator, 'plugins', {
//                    get: () => [
//                        {
//                            0: { type: 'application/x-google-chrome-pdf', suffixes: 'pdf', description: 'Portable Document Format', enabledPlugin: Plugin },
//                            description: 'Portable Document Format',
//                            filename: 'internal-pdf-viewer',
//                            length: 1,
//                            name: 'Chrome PDF Plugin'
//                        }
//                    ]
//                });
//            ");

//            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting navigation to {targetUrl}");
//            await page.GotoAsync(targetUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

//            // Simulate human behavior
//            await SimulateHumanBehavior(page, instanceId, iteration);

//            // Keep the browser open for a random duration between 5-10 seconds after behavior simulation
//            var random = new Random();
//            await Task.Delay(random.Next(5000, 10000));

//            await browser.CloseAsync();
//            Console.WriteLine($"Instance {instanceId}-{iteration}: Session completed successfully");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Instance {instanceId}-{iteration} error: {ex.Message}");
//        }
//    }

//    private static async Task SimulateHumanBehavior(IPage page, int instanceId, int iteration)
//    {
//        try
//        {
//            var random = new Random();
//            var startTime = DateTime.Now;

//            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting initial scroll behavior simulation");

//            // First round of scrolling
//            await PerformScrollingBehavior(page, instanceId, iteration, "Initial");

//            // Try to click on the CSS selector
//            if (!string.IsNullOrEmpty(Selector1))
//            {
//                await AttemptSelectorClick(Selector1, page, instanceId, iteration);
//                if (!string.IsNullOrEmpty(Selector2))
//                {
//                    await AttemptSelectorClick(Selector2, page, instanceId, iteration);
//                    if (!string.IsNullOrEmpty(Selector3))
//                    { 
//                    await AttemptSelectorClick(Selector3, page, instanceId, iteration);
//                        if (!string.IsNullOrEmpty(Selector4))
//                        { 
//                    await AttemptSelectorClick(Selector4, page, instanceId, iteration);
//                            if (!string.IsNullOrEmpty(Selector5))
//                            {
//                                await AttemptSelectorClick(Selector5, page, instanceId, iteration);

//                            }
//                        }
//                    }
//                }
//            }
//            // Second round of scrolling after clicking


//            var elapsedTime = (DateTime.Now - startTime).TotalSeconds;
//            Console.WriteLine($"Instance {instanceId}-{iteration}: Completed full behavior simulation in {elapsedTime:F1} seconds");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Instance {instanceId}-{iteration}: Behavior simulation error: {ex.Message}");
//        }
//    }

//    private static async Task PerformScrollingBehavior(IPage page, int instanceId, int iteration, string phase)
//    {
//        try
//        {
//            var random = new Random();
//            var startTime = DateTime.Now;

//            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting {phase} scroll behavior simulation");
//            await Task.Delay(random.Next(minMin*1000, maxMin*1000));

//            // Phase 1: Scroll down gradually (20 seconds)
//            var scrollDownEndTime = startTime.AddSeconds(scrollTime);
//            var totalScrollDown = 0;

//            while (DateTime.Now < scrollDownEndTime)
//            {
//                // Random scroll down amount between 200-500 pixels
//                var scrollAmount = random.Next(200, 500);

//                // Use smooth scrolling
//                await page.EvaluateAsync($"window.scrollBy({{top: {scrollAmount}, behavior: 'smooth'}})");
//                totalScrollDown += scrollAmount;

//                // Random delay between scrolls (800ms to 2 seconds for smoother experience)
//                await Task.Delay(random.Next(800, 2000));

//                Console.WriteLine($"Instance {instanceId}-{iteration}: {phase} - Scrolled down {scrollAmount}px (total: {totalScrollDown}px)");
//            }

//            // Small pause between scrolling phases
//            await Task.Delay(random.Next(1000, 2000));

//            // Phase 2: Scroll back to top (20 seconds)
//            var scrollUpEndTime = startTime.AddSeconds(40);
//            var totalScrollUp = 0;

//            while (DateTime.Now < scrollUpEndTime)
//            {
//                // Random scroll up amount between 200-500 pixels
//                var scrollAmount = random.Next(200, 500);

//                // Use smooth scrolling upward
//                await page.EvaluateAsync($"window.scrollBy({{top: -{scrollAmount}, behavior: 'smooth'}})");
//                totalScrollUp += scrollAmount;

//                // Random delay between scrolls
//                await Task.Delay(random.Next(800, 2000));

//                Console.WriteLine($"Instance {instanceId}-{iteration}: {phase} - Scrolled up {scrollAmount}px (total up: {totalScrollUp}px)");
//            }

//            // Ensure we're back at the top with smooth scrolling
//            await page.EvaluateAsync("window.scrollTo({top: 0, behavior: 'smooth'})");
//            await Task.Delay(1000);

//            // Add some final mouse movements to simulate reading at the top
//            for (int i = 0; i < random.Next(2, 4); i++)
//            {
//                var x = random.Next(100, 700);
//                var y = random.Next(100, 300);
//                await page.Mouse.MoveAsync(x, y);
//                await Task.Delay(random.Next(300, 800));
//            }

//            Console.WriteLine($"Instance {instanceId}-{iteration}: Completed {phase} scroll behavior");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Instance {instanceId}-{iteration}: {phase} scroll behavior error: {ex.Message}");
//        }
//    }

//    private static async Task AttemptSelectorClick(string Selector,IPage page, int instanceId, int iteration)
//    {
//        try
//        {
//            Console.WriteLine($"Instance {instanceId}-{iteration}: Attempting to find and click CSS selector: {Selector}");

//            var element = page.Locator(Selector);
//            await element.ClickAsync();
//            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting second round of scrolling");
//            await PerformScrollingBehavior(page, instanceId, iteration, nameof(Selector));
//        }
//        catch (TimeoutException)
//        {
//            Console.WriteLine($"Instance {instanceId}-{iteration}: Timeout waiting for CSS selector '#post-22 > div > div > header > h4 > a' - element may not exist");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Instance {instanceId}-{iteration}: Error clicking CSS selector '#post-22 > div > div > header > h4 > a': {ex.Message}");
//        }
//    }
//}