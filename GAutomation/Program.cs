using Microsoft.Playwright;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

class Program
{
    private static readonly ConcurrentDictionary<int, bool> _activeInstances = new ConcurrentDictionary<int, bool>();
    private static volatile bool _isRunning = true;
    private static ConfigData config;

    public class ConfigData
    {
        public ProxySettings Proxy { get; set; }
        public string TargetUrl { get; set; }
        public int NoOfInstances { get; set; }
        public int LoopCount { get; set; }
        public List<ActionItem> ActionSequence { get; set; }
    }

    public class ProxySettings
    {
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

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

        // Load configuration from file
        if (!LoadConfiguration())
        {
            Console.WriteLine("Failed to load configuration. Exiting...");
            return;
        }

        // Display configuration summary
        DisplayConfigurationSummary();

        Console.WriteLine("\nStarting automation...");

        // Start the main processing
        var processingTask = ProcessInstances(config.TargetUrl);

        // Wait for user input to stop
        Console.WriteLine("\nPress 'Q' to quit...");
        while (Console.ReadKey(true).Key != ConsoleKey.Q) { }

        _isRunning = false;
        await processingTask;
    }

    private static bool LoadConfiguration()
    {
        try
        {
            string configPath = "config.json";

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Configuration file '{configPath}' not found. Creating default configuration...");
                CreateDefaultConfiguration(configPath);
                Console.WriteLine($"Default configuration created at '{configPath}'. Please edit it and run the program again.");
                return false;
            }

            string jsonContent = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<ConfigData>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Validate configuration
            if (!ValidateConfiguration())
            {
                return false;
            }

            // Ensure URL has proper protocol
            if (!config.TargetUrl.StartsWith("http"))
            {
                config.TargetUrl = "https://" + config.TargetUrl;
            }

            if (!config.Proxy.Server.StartsWith("http"))
            {
                config.Proxy.Server = "https://" + config.Proxy.Server;
            }

            // Generate descriptions for actions if missing
            foreach (var action in config.ActionSequence)
            {
                if (string.IsNullOrEmpty(action.Description))
                {
                    action.Description = GenerateActionDescription(action);
                }
            }

            Console.WriteLine("Configuration loaded successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            return false;
        }
    }

    private static bool ValidateConfiguration()
    {
        if (config == null)
        {
            Console.WriteLine("Configuration is null");
            return false;
        }

        if (config.Proxy == null || string.IsNullOrEmpty(config.Proxy.Server) ||
            string.IsNullOrEmpty(config.Proxy.Username) || string.IsNullOrEmpty(config.Proxy.Password))
        {
            Console.WriteLine("Invalid proxy configuration. Please check server, username, and password.");
            return false;
        }

        if (string.IsNullOrEmpty(config.TargetUrl))
        {
            Console.WriteLine("Target URL is required");
            return false;
        }

        if (config.NoOfInstances <= 0)
        {
            Console.WriteLine("Number of instances must be greater than 0");
            return false;
        }

        if (config.LoopCount <= 0)
        {
            Console.WriteLine("Loop count must be greater than 0");
            return false;
        }

        if (config.ActionSequence == null || config.ActionSequence.Count == 0)
        {
            Console.WriteLine("At least one action is required in ActionSequence");
            return false;
        }

        // Validate each action
        foreach (var action in config.ActionSequence)
        {
            if (string.IsNullOrEmpty(action.Type))
            {
                Console.WriteLine("Action type is required for all actions");
                return false;
            }

            switch (action.Type.ToLower())
            {
                case "wait":
                case "scroll_down":
                case "scroll_up":
                    if (action.Duration <= 0)
                    {
                        Console.WriteLine($"Duration must be greater than 0 for {action.Type} actions");
                        return false;
                    }
                    break;
                case "click":
                    if (string.IsNullOrEmpty(action.Selector))
                    {
                        Console.WriteLine("Selector is required for click actions");
                        return false;
                    }
                    break;
                default:
                    Console.WriteLine($"Unknown action type: {action.Type}");
                    return false;
            }
        }

        return true;
    }

    private static void CreateDefaultConfiguration(string configPath)
    {
        var defaultConfig = new ConfigData
        {
            Proxy = new ProxySettings
            {
                Server = "https://geo.iproyal.com:12321",
                Username = "your_proxy_username",
                Password = "your_proxy_password"
            },
            TargetUrl = "https://www.google.com",
            NoOfInstances = 1,
            LoopCount = 1,
            ActionSequence = new List<ActionItem>
            {
                new ActionItem
                {
                    Type = "wait",
                    Duration = 5,
                    Description = "Wait for 5 seconds"
                },
                new ActionItem
                {
                    Type = "scroll_down",
                    Duration = 20,
                    Description = "Scroll down for 20 seconds"
                },
                new ActionItem
                {
                    Type = "scroll_up",
                    Duration = 20,
                    Description = "Scroll up for 20 seconds"
                },
                new ActionItem
                {
                    Type = "click",
                    Selector = "#example-button",
                    Description = "Click on example button"
                }
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string jsonString = JsonSerializer.Serialize(defaultConfig, options);
        File.WriteAllText(configPath, jsonString);
    }

    private static string GenerateActionDescription(ActionItem action)
    {
        return action.Type.ToLower() switch
        {
            "wait" => $"Wait for {action.Duration} seconds",
            "scroll_down" => $"Scroll down for {action.Duration} seconds",
            "scroll_up" => $"Scroll up for {action.Duration} seconds",
            "click" => $"Click on selector: {action.Selector}",
            _ => $"Unknown action: {action.Type}"
        };
    }

    private static void DisplayConfigurationSummary()
    {
        Console.WriteLine("\n=== Configuration Summary ===");
        Console.WriteLine($"Target URL: {config.TargetUrl}");
        Console.WriteLine($"Proxy Server: {config.Proxy.Server}");
        Console.WriteLine($"Proxy Username: {config.Proxy.Username}");
        Console.WriteLine($"Browser Instances: {config.NoOfInstances}");
        Console.WriteLine($"Iterations per Instance: {config.LoopCount}");
        Console.WriteLine($"Total Actions per Session: {config.ActionSequence.Count}");
        Console.WriteLine("\nAction Sequence:");
        for (int i = 0; i < config.ActionSequence.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {config.ActionSequence[i].Description}");
        }
    }

    private static async Task ProcessInstances(string targetUrl)
    {
        var instanceTasks = new List<Task>();

        for (int instanceId = 0; instanceId < config.NoOfInstances; instanceId++)
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
        Console.WriteLine($"Instance {instanceId}: Started with {config.LoopCount} iterations");

        for (int iteration = 0; iteration < config.LoopCount && _isRunning; iteration++)
        {
            try
            {
                Console.WriteLine($"Instance {instanceId}: Starting iteration {iteration + 1}/{config.LoopCount}");
                await RunSingleBrowserSession(targetUrl, instanceId, iteration);

                if (iteration < config.LoopCount - 1 && _isRunning)
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
                Server = config.Proxy.Server,
                Username = config.Proxy.Username,
                Password = config.Proxy.Password,
            };

            var contextOptions = BrowserFingerprintManager.CreateContextOptions(fingerprintProfile, proxy);
            var context = await browser.NewContextAsync(contextOptions);

            await BrowserFingerprintManager.ApplyFingerprintToContext(context, fingerprintProfile);

            var page = await context.NewPageAsync();

            // Add stealth measures
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
            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting action sequence ({config.ActionSequence.Count} actions)");

            for (int i = 0; i < config.ActionSequence.Count; i++)
            {
                if (!_isRunning) break;

                var action = config.ActionSequence[i];
                Console.WriteLine($"Instance {instanceId}-{iteration}: Executing action {i + 1}/{config.ActionSequence.Count} - {action.Description}");

                switch (action.Type.ToLower())
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
                if (i < config.ActionSequence.Count - 1)
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