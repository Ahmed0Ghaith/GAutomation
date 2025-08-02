using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;

public class BrowserFingerprintManager
{
    private static readonly Random _random = new Random();

    // Common user agents for different browsers and platforms
    private static readonly string[] _userAgents = {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/119.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:109.0) Gecko/20100101 Firefox/119.0",
        "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/119.0"
    };

    private static readonly string[] _platforms = {
        "Win32", "MacIntel", "Linux x86_64", "Linux i686"
    };

    //private static readonly string[] _languages = {
    //    "en-US,en;q=0.9",
    //    "en-GB,en;q=0.9",
    //    "en-CA,en;q=0.9",
    //    "en-AU,en;q=0.9",
    //    "en-US,en;q=0.9,es;q=0.8",
    //    "en-GB,en;q=0.9,fr;q=0.8"
    //};

    //private static readonly string[] _timezones = {
    //    "America/New_York",
    //    "America/Los_Angeles",
    //    "America/Chicago",
    //    "Europe/London",
    //    "Europe/Berlin",
    //    "Europe/Paris",
    //    "Australia/Sydney",
    //    "America/Toronto"
    //};

    private static readonly ViewportSize[] _viewportSizes = {
        new ViewportSize { Width = 1920, Height = 1080 },
        new ViewportSize { Width = 1366, Height = 768 },
        new ViewportSize { Width = 1440, Height = 900 },
        new ViewportSize { Width = 1536, Height = 864 },
        new ViewportSize { Width = 1280, Height = 720 },
        new ViewportSize { Width = 1600, Height = 900 }
    };

    private static readonly string[] _screenResolutions = {
        "1920x1080", "1366x768", "1440x900", "1536x864", "1280x720", "1600x900"
    };

    public class FingerprintProfile
    {
        public string UserAgent { get; set; }
        public string Platform { get; set; }
        //public string Language { get; set; }
        //public string Timezone { get; set; }
        public ViewportSize ViewportSize { get; set; }
        public string ScreenResolution { get; set; }
        public int ColorDepth { get; set; }
        public int DevicePixelRatio { get; set; }
        public bool CookiesEnabled { get; set; }
        public bool DoNotTrack { get; set; }
        public Dictionary<string, string> ExtraHeaders { get; set; }
        public string[] BrowserArgs { get; set; }
    }

    public static FingerprintProfile GenerateRandomFingerprint(int instanceId)
    {
        var userAgent = _userAgents[_random.Next(_userAgents.Length)];
        var viewport = _viewportSizes[_random.Next(_viewportSizes.Length)];
        var screenRes = _screenResolutions[_random.Next(_screenResolutions.Length)];

        return new FingerprintProfile
        {
            UserAgent = userAgent,
            Platform = _platforms[_random.Next(_platforms.Length)],
         //   Language = _languages[_random.Next(_languages.Length)],
         //   Timezone = _timezones[_random.Next(_timezones.Length)],
            ViewportSize = viewport,
            ScreenResolution = screenRes,
            ColorDepth = _random.Next(2) == 0 ? 24 : 32,
            DevicePixelRatio = _random.Next(2) == 0 ? 1 : 2,
            CookiesEnabled = true,
            DoNotTrack = _random.Next(2) == 0,
            ExtraHeaders = GenerateRandomHeaders(),
            BrowserArgs = GenerateRandomBrowserArgs(instanceId)
        };
    }

    private static Dictionary<string, string> GenerateRandomHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            ["sec-ch-ua"] = GenerateSecChUa(),
            ["sec-ch-ua-mobile"] = "?0",
            ["sec-ch-ua-platform"] = $"\"{GetRandomPlatformForHeader()}\"",
            ["sec-fetch-dest"] = "document",
            ["sec-fetch-mode"] = "navigate",
            ["sec-fetch-site"] = "none",
            ["sec-fetch-user"] = "?1",
            ["upgrade-insecure-requests"] = "1"
        };

        // Randomly add some optional headers
        if (_random.Next(2) == 0)
        {
            headers["dnt"] = "1";
        }

        return headers;
    }

    private static string GenerateSecChUa()
    {
        var brands = new[]
        {
            "\"Google Chrome\";v=\"119\", \"Chromium\";v=\"119\", \"Not?A_Brand\";v=\"24\"",
            "\"Google Chrome\";v=\"118\", \"Chromium\";v=\"118\", \"Not=A?Brand\";v=\"99\"",
            "\"Chromium\";v=\"119\", \"Not?A_Brand\";v=\"24\"",
            "\"Google Chrome\";v=\"119\", \"Not;A=Brand\";v=\"24\""
        };
        return brands[_random.Next(brands.Length)];
    }

    private static string GetRandomPlatformForHeader()
    {
        var platforms = new[] { "Windows", "macOS", "Linux" };
        return platforms[_random.Next(platforms.Length)];
    }

    private static string[] GenerateRandomBrowserArgs(int instanceId)
    {
        var baseArgs = new List<string>
        {
            "--disable-blink-features=AutomationControlled",
            "--disable-features=IsolateOrigins,site-per-process",
            "--no-first-run",
            "--no-default-browser-check",
            "--disable-extensions-except",
            "--disable-plugins-discovery",
            "--disable-web-security",
            "--disable-features=VizDisplayCompositor",
            $"--window-position={instanceId * 100 + _random.Next(50)},{instanceId * 50 + _random.Next(30)}"
        };

        // Add random canvas and webgl fingerprint modifications
        if (_random.Next(2) == 0)
        {
            baseArgs.Add("--disable-reading-from-canvas");
        }

        if (_random.Next(2) == 0)
        {
            baseArgs.Add("--disable-webgl");
        }

        // Random memory and CPU related args
        var memoryArgs = new[]
        {
            "--max_old_space_size=4096",
            "--max_old_space_size=2048",
            "--max_old_space_size=8192"
        };
        baseArgs.Add(memoryArgs[_random.Next(memoryArgs.Length)]);

        return baseArgs.ToArray();
    }

    public static async Task ApplyFingerprintToContext(IBrowserContext context, FingerprintProfile profile)
    {
        // Add initialization script to modify navigator properties
        await context.AddInitScriptAsync($@"
            // Override navigator properties
            Object.defineProperty(navigator, 'userAgent', {{
                get: () => '{profile.UserAgent}'
            }});
            
            Object.defineProperty(navigator, 'platform', {{
                get: () => '{profile.Platform}'
            }});
            
          
            
            Object.defineProperty(navigator, 'cookieEnabled', {{
                get: () => {profile.CookiesEnabled.ToString().ToLower()}
            }});
            
            Object.defineProperty(navigator, 'doNotTrack', {{
                get: () => '{(profile.DoNotTrack ? "1" : "0")}'
            }});

            // Override screen properties
            Object.defineProperty(screen, 'width', {{
                get: () => {profile.ViewportSize.Width}
            }});
            
            Object.defineProperty(screen, 'height', {{
                get: () => {profile.ViewportSize.Height}
            }});
            
            Object.defineProperty(screen, 'colorDepth', {{
                get: () => {profile.ColorDepth}
            }});
            
            Object.defineProperty(window, 'devicePixelRatio', {{
                get: () => {profile.DevicePixelRatio}
            }});

            // Add canvas fingerprint randomization
            const getImageData = HTMLCanvasElement.prototype.toDataURL;
            HTMLCanvasElement.prototype.toDataURL = function(format) {{
                const canvas = this;
                const ctx = canvas.getContext('2d');
                const imageData = ctx.getImageData(0, 0, 1, 1);
                const originalData = imageData.data;
                
                // Add slight random noise to canvas data
                for (let i = 0; i < originalData.length; i += 4) {{
                    originalData[i] = Math.min(255, originalData[i] + Math.floor(Math.random() * 3) - 1);
                    originalData[i + 1] = Math.min(255, originalData[i + 1] + Math.floor(Math.random() * 3) - 1);
                    originalData[i + 2] = Math.min(255, originalData[i + 2] + Math.floor(Math.random() * 3) - 1);
                }}
                
                ctx.putImageData(imageData, 0, 0);
                return getImageData.apply(this, arguments);
            }};

            // Randomize WebGL fingerprint
            const getParameter = WebGLRenderingContext.prototype.getParameter;
            WebGLRenderingContext.prototype.getParameter = function(parameter) {{
                if (parameter === 37445) {{ // UNMASKED_VENDOR_WEBGL
                    return 'Intel Inc.';
                }}
                if (parameter === 37446) {{ // UNMASKED_RENDERER_WEBGL
                    const renderers = ['Intel Iris OpenGL Engine', 'ANGLE (Intel, Intel(R) HD Graphics 630 Direct3D11 vs_5_0 ps_5_0)', 'NVIDIA GeForce GTX 1060'];
                    return renderers[Math.floor(Math.random() * renderers.length)];
                }}
                return getParameter.apply(this, arguments);
            }};
        ");
    }

    public static BrowserNewContextOptions CreateContextOptions(FingerprintProfile profile, Proxy proxy = null)
    {
        var options = new BrowserNewContextOptions
        {
            UserAgent = profile.UserAgent,
            ViewportSize = profile.ViewportSize,
            ExtraHTTPHeaders = profile.ExtraHeaders,
         //   Locale = profile.Language.Split(',')[0],
          //  TimezoneId = profile.Timezone,
            DeviceScaleFactor = profile.DevicePixelRatio
        };

        if (proxy != null)
        {
            options.Proxy = proxy;
        }

        return options;
    }
}