

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

        if (string.IsNullOrEmpty(targetUrl))
        {
            Console.WriteLine("No URL provided. Using default website.");
            targetUrl = "https://www.google.com";
        }

        if (!targetUrl.StartsWith("http"))
        {
            targetUrl = "https://" + targetUrl;
        }

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
            if (_activeInstances.Count < 5)
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
            using var playwright = await Playwright.CreateAsync();
            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 50,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--disable-features=IsolateOrigins,site-per-process",
                    $"--window-position={instanceId * 100},{instanceId * 50}", // Stagger windows
                    "--window-size=800,600"
                }
            };

            await using var browser = await playwright.Chromium.LaunchAsync(launchOptions);
            var contextOptions = new BrowserNewContextOptions
            {
                Proxy = new Proxy
                {
                    Server = "https://geo.iproyal.com:12321",
                    Username = "kx25pY99mFbmORvY",
                    Password = "MhTTC3Ajh8bHkk8B_country-gb"
                },
                ViewportSize = new ViewportSize { Width = 800, Height = 600 }
            };

            var context = await browser.NewContextAsync(contextOptions);
            var page = await context.NewPageAsync();

            Console.WriteLine($"Instance {instanceId}: Starting navigation to {targetUrl}");
            await page.GotoAsync(targetUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

            // Keep the browser open for a random duration between 30-60 seconds
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
}
//using Microsoft.Playwright;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Threading.Tasks;

//class Program
//{
//    public static async Task Main()
//    {
//        // Get user input for target website
//        Console.WriteLine("Enter the website URL you want to visit:");
//        string targetUrl = Console.ReadLine();
//        int loopCount = int.Parse(Console.ReadLine());
//        if (string.IsNullOrEmpty(targetUrl))
//        {
//            Console.WriteLine("No URL provided. Using default website.");
//            targetUrl = "https://www.google.com";
//        }

//        if (!targetUrl.StartsWith("http"))
//        {
//            targetUrl = "https://" + targetUrl;
//        }

//        // Initialize Playwright
//        using var playwright = await Playwright.CreateAsync();

//        // Configure browser launch options with proxy and fingerprinting
//        var launchOptions = new BrowserTypeLaunchOptions
//        {
//            Headless = false,
//            SlowMo = 50,
//            Args = new[]
//            {
//                "--disable-blink-features=AutomationControlled",
//                "--disable-features=IsolateOrigins,site-per-process",
//                "--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36",
//                "--window-size=1920,1080"
//            }
//        };

//        // Launch browser
//        var browser = await playwright.Chromium.LaunchAsync(launchOptions);

//        // Create a context with specific options for fingerprinting
//        var contextOptions = new BrowserNewContextOptions
//        {
//            Proxy = new Proxy
//            {
//                Server = "https://geo.iproyal.com:12321",
//                Username = "kx25pY99mFbmORvY",
//                Password = "MhTTC3Ajh8bHkk8B_country-gb"
//            },
//            ViewportSize = new ViewportSize
//            {
//                Width = 1920,
//                Height = 1080
//            },
//            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36",
//          //  Locale = "en-GB",
//          //  TimezoneId = "Europe/London",
//            //Geolocation = new Geolocation
//            //{
//            //    Longitude = (float)-0.1278,
//            //    Latitude = 51.5074,
//            //    Accuracy = 1
//            //},
//            HasTouch = false,
//            DeviceScaleFactor = 1,
//            IsMobile = false,
//            ColorScheme = ColorScheme.Light,
//            ReducedMotion = ReducedMotion.NoPreference,
//            ForcedColors = ForcedColors.None,
//            AcceptDownloads = true
//        };

//        var context = await browser.NewContextAsync(contextOptions);

//        // Enable JavaScript permissions
//        await context.GrantPermissionsAsync(new[] { "geolocation", "notifications" });

//        // Create a new page
//        var page = await context.NewPageAsync();

//        try
//        {
//            // First, visit some websites to add cookies and create browsing history
//            //string[] seedSites = {
//            //    "https://www.visitbritain.com/gb/en",
//            //    "https://www.gov.uk/government/organisations/department-for-education",
//            //    "https://www.aviva.co.uk/insurance/"
//            //};

//            // Visit seed websites and scroll to simulate real browsing
//            //foreach (var site in seedSites)
//            //{
//            //    Console.WriteLine($"Visiting {site} to build browsing profile...");
//            //    await page.GotoAsync(site, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

//            //    // Scroll down and up to simulate real user behavior
//            //    await page.EvaluateAsync(@"() => {
//            //        window.scrollTo(0, document.body.scrollHeight * 0.3);
//            //    }");
//            //    await Task.Delay(1000);

//            //    await page.EvaluateAsync(@"() => {
//            //        window.scrollTo(0, document.body.scrollHeight * 0.7);
//            //    }");
//            //    await Task.Delay(1000);

//            //    await page.EvaluateAsync(@"() => {
//            //        window.scrollTo(0, 0);
//            //    }");
//            //    await Task.Delay(1000);

//            //    // Get cookies from this site to build up profile
//            //    var cookies = await context.CookiesAsync(new[] { new URL(site).Host });
//            //    Console.WriteLine($"Added {cookies.Length} cookies from {site}");
//            //}

//            //// Override specific fingerprint values via JavaScript
//            //await page.AddInitScriptAsync(@"
//            //    () => {
//            //        // Override navigator properties
//            //        Object.defineProperty(navigator, 'webdriver', {
//            //            get: () => false
//            //        });

//            //        // Override plugins
//            //        Object.defineProperty(navigator, 'plugins', {
//            //            get: () => {
//            //                return [
//            //                    {
//            //                        0: {type: 'application/pdf'},
//            //                        description: 'Portable Document Format',
//            //                        filename: 'internal-pdf-viewer',
//            //                        length: 1,
//            //                        name: 'PDF Viewer'
//            //                    },
//            //                    {
//            //                        0: {type: 'application/x-google-chrome-pdf'},
//            //                        description: 'Chrome PDF Plugin',
//            //                        filename: 'internal-pdf-viewer',
//            //                        length: 1,
//            //                        name: 'Chrome PDF Plugin'
//            //                    }
//            //                ];
//            //            }
//            //        });

//            //        // Override canvas fingerprinting
//            //        const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
//            //        HTMLCanvasElement.prototype.toDataURL = function(type) {
//            //            if (type === 'image/png' && this.width === 220 && this.height === 30) {
//            //                // This is likely a fingerprinting attempt
//            //                return originalToDataURL.apply(this, [type]);
//            //            }
//            //            return originalToDataURL.apply(this, arguments);
//            //        };

//            //        // Override WebGL fingerprinting
//            //        const getParameter = WebGLRenderingContext.prototype.getParameter;
//            //        WebGLRenderingContext.prototype.getParameter = function(parameter) {
//            //            // VENDOR and RENDERER are commonly used for fingerprinting
//            //            if (parameter === 37445) {
//            //                return 'Intel Inc.';
//            //            }
//            //            if (parameter === 37446) {
//            //                return 'Intel Iris Pro Graphics';
//            //            }
//            //            return getParameter.apply(this, arguments);
//            //        };
//            //    }
//            //");

//            // Now visit the user's requested website
//            Console.WriteLine($"Navigating to target website: {targetUrl}");

//            // Navigate to the user-specified URL
//            await page.GotoAsync(targetUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

//            Console.WriteLine("Page loaded successfully");

//            // Wait for any additional resources to load
//            await Task.Delay(10000);

//            // Check IP address
//            //Console.WriteLine("Checking IP address...");
//            //await page.GotoAsync("https://whatismyipaddress.com/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
//            //var ipElement = await page.QuerySelectorAsync("a[href^='/ip/']");
//            //string ip = await ipElement.TextContentAsync();
//            //Console.WriteLine($"Current IP: {ip}");

//            // Return to the target website
//            await page.GotoAsync(targetUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

//            // Implement smooth scrolling behavior
//            Console.WriteLine("Starting scrolling simulation...");

//            // Scroll down to end of page over 5 seconds
//            //await page.EvaluateAsync(@"() => {
//            //    return new Promise((resolve) => {
//            //        const totalHeight = document.body.scrollHeight;
//            //        const duration = 5000; // 5 seconds
//            //        const scrollStep = totalHeight / (duration / 100);
//            //        let currentPosition = 0;

//            //        const scrollInterval = setInterval(() => {
//            //            if (currentPosition < totalHeight) {
//            //                window.scrollBy(0, scrollStep);
//            //                currentPosition += scrollStep;
//            //            } else {
//            //                clearInterval(scrollInterval);
//            //                resolve();
//            //            }
//            //        }, 100);
//            //    });
//            //}");

//            //// Wait a moment at the bottom
//            //await Task.Delay(2000);

//            //// Scroll back up over 3 seconds
//            //await page.EvaluateAsync(@"() => {
//            //    return new Promise((resolve) => {
//            //        const duration = 3000; // 3 seconds
//            //        const currentPosition = window.pageYOffset;
//            //        const scrollStep = currentPosition / (duration / 100);
//            //        let remaining = currentPosition;

//            //        const scrollInterval = setInterval(() => {
//            //            if (remaining > 0) {
//            //                window.scrollBy(0, -scrollStep);
//            //                remaining -= scrollStep;
//            //            } else {
//            //                clearInterval(scrollInterval);
//            //                window.scrollTo(0, 0);
//            //                resolve();
//            //            }
//            //        }, 100);
//            //    });
//            //}");

//            Console.WriteLine("Website exploration complete!");
//            Console.WriteLine("Press any key to exit the browser...");
//            Console.ReadKey();
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error during browsing: {ex.Message}");
//            Console.WriteLine($"Stack trace: {ex.StackTrace}");
//        }
//        finally
//        {
//            // Clean up
//            await browser.CloseAsync();
//        }
//    }
//}