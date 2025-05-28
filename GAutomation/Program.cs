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
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
    private static readonly ConcurrentDictionary<int, bool> _activeInstances = new ConcurrentDictionary<int, bool>();
    private static volatile bool _isRunning = true;
    private static Config ?_config;

    public static async Task Main()
    {
        // Load configuration
        _config = LoadConfig();

        if (_config == null)
        {
            Console.WriteLine("Failed to load configuration. Exiting...");
            return;
        }

        Console.WriteLine("Configuration loaded successfully!");
        Console.WriteLine($"Instances: {_config.NoOfInstances}, Loops: {_config.LoopCount}");
        Console.WriteLine($"Target URL: {_config.TargetUrl}");
        Console.WriteLine($"Proxy Server: {_config.Server}");

        if (!string.IsNullOrEmpty(_config.CssSelector))
        {
            Console.WriteLine($"CSS Selector: {_config.CssSelector}");
        }

        // Validate configuration
        if (string.IsNullOrEmpty(_config.TargetUrl))
        {
            Console.WriteLine("No target URL specified in config. Using default.");
            _config.TargetUrl = "https://www.google.com";
        }

        if (!_config.TargetUrl.StartsWith("http"))
        {
            _config.TargetUrl = "https://" + _config.TargetUrl;
        }

        if (!string.IsNullOrEmpty(_config.Server) && !_config.Server.StartsWith("http"))
        {
            _config.Server = "https://" + _config.Server;
        }

        Console.WriteLine($"Starting {_config.NoOfInstances} browser instances, each running {_config.LoopCount} iterations...");
        if (!string.IsNullOrEmpty(_config.CssSelector))
        {
            Console.WriteLine($"Will click on elements matching CSS selector: {_config.CssSelector}");
        }

        // Start the main processing
        var processingTask = ProcessInstances();

        // Wait for user input to stop
        Console.WriteLine("Press 'Q' to quit...");
        while (Console.ReadKey(true).Key != ConsoleKey.Q) { }

        _isRunning = false;
        await processingTask;
    }

    private static Config LoadConfig()
    {
        const string configFile = "config.json";

        try
        {
            if (File.Exists(configFile))
            {
                var json = File.ReadAllText(configFile);
                return JsonSerializer.Deserialize<Config>(json);
            }
            else
            {
                // Create default config file
                var defaultConfig = new Config();
                var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFile, json);

                Console.WriteLine($"Created default config file: {configFile}");
                Console.WriteLine("Please edit the config file and restart the application.");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading config: {ex.Message}");
            return null;
        }
    }

    private static async Task ProcessInstances()
    {
        var instanceTasks = new List<Task>();
        var random = new Random();

        // Create the specified number of browser instances with staggered start times
        for (int instanceId = 0; instanceId < _config.NoOfInstances; instanceId++)
        {
            if (!_isRunning) break;

            _activeInstances[instanceId] = true;

            // Add random delay between starting instances to avoid pattern detection
            if (instanceId > 0)
            {
                await Task.Delay(random.Next(_config.MinDelayBetweenInstances, _config.MaxDelayBetweenInstances));
            }

            var task = RunBrowserInstanceWithIterations(instanceId);
            instanceTasks.Add(task);
        }

        // Wait for all instances to complete
        await Task.WhenAll(instanceTasks);
    }

    private static async Task RunBrowserInstanceWithIterations(int instanceId)
    {
        Console.WriteLine($"Instance {instanceId}: Started with {_config.LoopCount} iterations");
        var random = new Random();

        for (int iteration = 0; iteration < _config.LoopCount && _isRunning; iteration++)
        {
            try
            {
                Console.WriteLine($"Instance {instanceId}: Starting iteration {iteration + 1}/{_config.LoopCount}");
                await RunSingleBrowserSession(instanceId, iteration);

                // Random delay between iterations for the same instance
                if (iteration < _config.LoopCount - 1 && _isRunning)
                {
                    await Task.Delay(random.Next(_config.MinDelayBetweenInstances, _config.MaxDelayBetweenInstances));
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

    private static async Task RunSingleBrowserSession(int instanceId, int iteration)
    {
        try
        {
            // Generate unique fingerprint for this session
            var fingerprintProfile = BrowserFingerprintManager.GenerateRandomFingerprint(instanceId * 1000 + iteration);

            Console.WriteLine($"Instance {instanceId}-{iteration}: Generated fingerprint - UA: {fingerprintProfile.UserAgent.Substring(0, Math.Min(50, fingerprintProfile.UserAgent.Length))}...");
            Console.WriteLine($"Instance {instanceId}-{iteration}: Platform: {fingerprintProfile.Platform}, Viewport: {fingerprintProfile.ViewportSize.Width}x{fingerprintProfile.ViewportSize.Height}");

            using var playwright = await Playwright.CreateAsync();

            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = _config.HeadlessMode,
                SlowMo = fingerprintProfile.SlowMo,
                Args = fingerprintProfile.BrowserArgs
            };

            await using var browser = await playwright.Chromium.LaunchAsync(launchOptions);

            // Create proxy configuration if specified
            Proxy proxy = null;
            if (!string.IsNullOrEmpty(_config.Server) && !string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
            {
                proxy = new Proxy
                {
                    Server = _config.Server,
                    Username = _config.Username,
                    Password = _config.Password,
                };
            }

            // Create context with fingerprint profile
            var contextOptions = BrowserFingerprintManager.CreateContextOptions(fingerprintProfile, proxy);
            var context = await browser.NewContextAsync(contextOptions);

            // Apply additional fingerprint modifications
            await BrowserFingerprintManager.ApplyFingerprintToContext(context, fingerprintProfile);

            var page = await context.NewPageAsync();

            // Enhanced stealth measures
            await ApplyAdvancedStealthMeasures(page);

            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting navigation to {_config.TargetUrl}");

            // Random delay before navigation
            await Task.Delay(new Random().Next(1000, 3000));

            await page.GotoAsync(_config.TargetUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60000
            });

            // Wait for page to fully load with random delay
            await Task.Delay(new Random().Next(2000, 5000));

            // Simulate human behavior
            await SimulateHumanBehavior(page, instanceId, iteration);

            // Keep the browser open for a random duration
            var random = new Random();
            await Task.Delay(random.Next(_config.MinPageStayTime, _config.MaxPageStayTime));

            await browser.CloseAsync();
            Console.WriteLine($"Instance {instanceId}-{iteration}: Session completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration} error: {ex.Message}");
        }
    }

    private static async Task ApplyAdvancedStealthMeasures(IPage page)
    {
        await page.AddInitScriptAsync(@"
            // Remove webdriver traces
            delete navigator.__proto__.webdriver;
            delete navigator.webdriver;
            
            // Override webdriver property
            Object.defineProperty(navigator, 'webdriver', {
                get: () => undefined
            });

            // Mock permissions API
            const originalQuery = window.navigator.permissions.query;
            window.navigator.permissions.query = (parameters) => (
                parameters.name === 'notifications' ?
                    Promise.resolve({ state: Notification.permission }) :
                    originalQuery(parameters)
            );

            // Mock languages with random variation
            const languages = [
                ['en-US', 'en'],
                ['en-GB', 'en'],
                ['en-CA', 'en', 'fr'],
                ['en-AU', 'en']
            ];
            const randomLang = languages[Math.floor(Math.random() * languages.length)];
            Object.defineProperty(navigator, 'languages', {
                get: () => randomLang
            });

            // Mock plugins with realistic data
            Object.defineProperty(navigator, 'plugins', {
                get: () => [
                    {
                        0: { type: 'application/x-google-chrome-pdf', suffixes: 'pdf', description: 'Portable Document Format' },
                        description: 'Portable Document Format',
                        filename: 'internal-pdf-viewer',
                        length: 1,
                        name: 'Chrome PDF Plugin'
                    },
                    {
                        0: { type: 'application/x-nacl', suffixes: 'nexe', description: 'Native Client Executable' },
                        description: 'Native Client',
                        filename: 'internal-nacl-plugin',
                        length: 1,
                        name: 'Native Client'
                    }
                ]
            });

            // Override chrome runtime
            if (!window.chrome) {
                window.chrome = {};
            }
            window.chrome.runtime = {
                onConnect: undefined,
                onMessage: undefined
            };

            // Add realistic connection properties
            Object.defineProperty(navigator, 'connection', {
                get: () => ({
                    effectiveType: '4g',
                    rtt: Math.floor(Math.random() * 100) + 50,
                    downlink: Math.random() * 10 + 1
                })
            });

            // Mock hardware concurrency
            Object.defineProperty(navigator, 'hardwareConcurrency', {
                get: () => [2, 4, 8, 12][Math.floor(Math.random() * 4)]
            });

            // Mock memory info for Chrome
            if ('memory' in performance) {
                Object.defineProperty(performance, 'memory', {
                    get: () => ({
                        usedJSHeapSize: Math.floor(Math.random() * 50000000) + 10000000,
                        totalJSHeapSize: Math.floor(Math.random() * 100000000) + 50000000,
                        jsHeapSizeLimit: 2172649472
                    })
                });
            }

            // Spoof Date.getTimezoneOffset with slight randomness
            const originalGetTimezoneOffset = Date.prototype.getTimezoneOffset;
            Date.prototype.getTimezoneOffset = function() {
                const offset = originalGetTimezoneOffset.call(this);
                return offset + (Math.random() > 0.5 ? 0 : 1); // Add slight variation
            };

            // Add mouse movement tracking prevention
            let mouseMovements = 0;
            document.addEventListener('mousemove', function() {
                mouseMovements++;
            });

            // Override getBoundingClientRect to add slight randomness
            const originalGetBoundingClientRect = Element.prototype.getBoundingClientRect;
            Element.prototype.getBoundingClientRect = function() {
                const rect = originalGetBoundingClientRect.call(this);
                const noise = () => Math.random() * 0.1 - 0.05;
                return {
                    x: rect.x + noise(),
                    y: rect.y + noise(),
                    width: rect.width + noise(),
                    height: rect.height + noise(),
                    top: rect.top + noise(),
                    right: rect.right + noise(),
                    bottom: rect.bottom + noise(),
                    left: rect.left + noise()
                };
            };
        ");
    }

    private static async Task SimulateHumanBehavior(IPage page, int instanceId, int iteration)
    {
        try
        {
            var random = new Random();
            var startTime = DateTime.Now;

            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting realistic human behavior simulation");

            // Simulate initial page interaction
            await SimulateInitialPageInteraction(page, instanceId, iteration);

            // First round of scrolling with more realistic patterns
            await PerformRealisticScrolling(page, instanceId, iteration, "Initial");

            // Try to click on the CSS selector
            await AttemptSelectorClick(page, instanceId, iteration);

            // Second round of scrolling after clicking
            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting second round of scrolling");
            await PerformRealisticScrolling(page, instanceId, iteration, "Second");

            // Simulate reading behavior at the end
            await SimulateReadingBehavior(page, instanceId, iteration);

            var elapsedTime = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine($"Instance {instanceId}-{iteration}: Completed full behavior simulation in {elapsedTime:F1} seconds");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Behavior simulation error: {ex.Message}");
        }
    }

    private static async Task SimulateInitialPageInteraction(IPage page, int instanceId, int iteration)
    {
        var random = new Random();

        // Random mouse movements to simulate user looking around
        for (int i = 0; i < random.Next(3, 6); i++)
        {
            var x = random.Next(100, 800);
            var y = random.Next(100, 400);
            await page.Mouse.MoveAsync(x, y);
            await Task.Delay(random.Next(300, 1000));
        }

        // Simulate checking different parts of the page
        await Task.Delay(random.Next(1000, 3000));
    }

    private static async Task PerformRealisticScrolling(IPage page, int instanceId, int iteration, string phase)
    {
        try
        {
            var random = new Random();
            var startTime = DateTime.Now;

            Console.WriteLine($"Instance {instanceId}-{iteration}: Starting {phase} realistic scroll behavior");

            // Get page height for more realistic scrolling
            var pageHeight = await page.EvaluateAsync<int>("document.body.scrollHeight");
            var viewportHeight = await page.EvaluateAsync<int>("window.innerHeight");

            // Phase 1: Scroll down with realistic patterns (varying speed and pauses)
            var currentPosition = 0;
            var targetPosition = Math.Max(pageHeight - viewportHeight, 0);
            var scrollPhaseTime = random.Next(15, 25); // 15-25 seconds for scrolling down

            var phaseEndTime = startTime.AddSeconds(scrollPhaseTime);

            while (DateTime.Now < phaseEndTime && currentPosition < targetPosition && _isRunning)
            {
                // Vary scroll amounts - sometimes small, sometimes larger
                int scrollAmount;
                if (random.NextDouble() < 0.3) // 30% chance of small scroll
                {
                    scrollAmount = random.Next(50, 150);
                }
                else if (random.NextDouble() < 0.7) // 40% chance of medium scroll
                {
                    scrollAmount = random.Next(200, 400);
                }
                else // 30% chance of larger scroll
                {
                    scrollAmount = random.Next(500, 800);
                }

                // Sometimes scroll up a bit (like reading something again)
                if (random.NextDouble() < 0.1 && currentPosition > 200)
                {
                    scrollAmount = -random.Next(100, 300);
                }

                currentPosition = Math.Max(0, Math.Min(targetPosition, currentPosition + scrollAmount));

                // Use smooth scrolling with varying behavior
                var scrollBehavior = random.NextDouble() < 0.7 ? "smooth" : "auto";
                await page.EvaluateAsync($"window.scrollTo({{top: {currentPosition}, behavior: '{scrollBehavior}'}})");

                // Realistic delays - sometimes pause longer (reading), sometimes quick scrolls
                int delay;
                if (random.NextDouble() < 0.2) // 20% chance of longer pause (reading)
                {
                    delay = random.Next(2000, 4000);
                }
                else if (random.NextDouble() < 0.3) // 30% chance of medium pause
                {
                    delay = random.Next(800, 1500);
                }
                else // 50% chance of quick scroll
                {
                    delay = random.Next(_config.MinScrollDelay, _config.MaxScrollDelay);
                }

                await Task.Delay(delay);

                // Occasionally move mouse during scrolling
                if (random.NextDouble() < 0.3)
                {
                    var x = random.Next(100, 700);
                    var y = random.Next(100, 600);
                    await page.Mouse.MoveAsync(x, y);
                }
            }

            // Pause at bottom
            await Task.Delay(random.Next(2000, 4000));

            // Phase 2: Scroll back up with different pattern
            var scrollUpEndTime = DateTime.Now.AddSeconds(random.Next(10, 20));

            while (DateTime.Now < scrollUpEndTime && currentPosition > 0 && _isRunning)
            {
                var scrollAmount = random.Next(200, 600);
                currentPosition = Math.Max(0, currentPosition - scrollAmount);

                await page.EvaluateAsync($"window.scrollTo({{top: {currentPosition}, behavior: 'smooth'}})");
                await Task.Delay(random.Next(600, 1800));
            }

            // Return to top with final smooth scroll
            await page.EvaluateAsync("window.scrollTo({top: 0, behavior: 'smooth'})");
            await Task.Delay(1000);

            Console.WriteLine($"Instance {instanceId}-{iteration}: Completed {phase} realistic scroll behavior");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: {phase} scroll behavior error: {ex.Message}");
        }
    }

    private static async Task SimulateReadingBehavior(IPage page, int instanceId, int iteration)
    {
        var random = new Random();

        // Simulate reading by staying still and occasional small movements
        for (int i = 0; i < random.Next(2, 5); i++)
        {
            // Small mouse movements
            var currentPos = await page.EvaluateAsync("({x: window.pageXOffset, y: window.pageYOffset})");

            var x = random.Next(200, 600);
            var y = random.Next(100, 400);
            await page.Mouse.MoveAsync(x, y);

            // Stay still for reading
            await Task.Delay(random.Next(2000, 5000));

            // Occasional small scroll adjustments
            if (random.NextDouble() < 0.5)
            {
                var smallScroll = random.Next(-100, 200);
                await page.EvaluateAsync($"window.scrollBy({{top: {smallScroll}, behavior: 'smooth'}})");
                await Task.Delay(500);
            }
        }
    }

    private static async Task AttemptSelectorClick(IPage page, int instanceId, int iteration)
    {
        if (string.IsNullOrEmpty(_config.CssSelector))
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: No CSS selector specified, skipping click");
            return;
        }

        try
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Attempting to find and click CSS selector: {_config.CssSelector}");

            // Wait for element to be available
            var element = page.Locator(_config.CssSelector);

            // Check if element exists and is visible
            if (await element.CountAsync() > 0)
            {
                // Scroll element into view first
                await element.ScrollIntoViewIfNeededAsync();
                await Task.Delay(new Random().Next(500, 1500));

                // Move mouse to element area first (more human-like)
                var boundingBox = await element.BoundingBoxAsync();
                if (boundingBox != null)
                {
                    var random = new Random();
                    var clickX = (float)(boundingBox.X + (boundingBox.Width * (0.3 + random.NextDouble() * 0.4)));
                    var clickY = (float)(boundingBox.Y + (boundingBox.Height * (0.3 + random.NextDouble() * 0.4)));

                    await page.Mouse.MoveAsync(clickX, clickY);
                    await Task.Delay(random.Next(200, 800));

                    // Perform the click
                    await element.ClickAsync();
                    Console.WriteLine($"Instance {instanceId}-{iteration}: Successfully clicked on CSS selector element");

                    // Wait after clicking
                    await Task.Delay(random.Next(1000, 3000));
                }
                else
                {
                    // Fallback click
                    await element.ClickAsync();
                    Console.WriteLine($"Instance {instanceId}-{iteration}: Successfully clicked on CSS selector element (fallback)");
                    await Task.Delay(new Random().Next(1000, 2000));
                }
            }
            else
            {
                Console.WriteLine($"Instance {instanceId}-{iteration}: Element with CSS selector '{_config.CssSelector}' not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Instance {instanceId}-{iteration}: Error clicking CSS selector '{_config.CssSelector}': {ex.Message}");
        }
    }
}