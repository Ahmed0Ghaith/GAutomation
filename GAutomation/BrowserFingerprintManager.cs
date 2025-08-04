using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;

public class BrowserFingerprintManager
{
    private static readonly Random _random = new Random();

    // Common user agents for different browsers and platforms (including Android)
    private static readonly string[] _userAgents = {
        // Desktop Chrome - Windows
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        
        // Desktop Chrome - macOS
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36",
        
        // Desktop Chrome - Linux
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        
        // Desktop Firefox
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/119.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:109.0) Gecko/20100101 Firefox/119.0",
        "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:109.0) Gecko/20100101 Firefox/119.0",
        
        // Android Chrome
        "Mozilla/5.0 (Linux; Android 13; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 12; SM-G998B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 12; Pixel 6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 13; SM-A536B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 11; SM-G973F) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 12; OnePlus 9) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Mobile Safari/537.36",
        
        // Android Firefox
        "Mozilla/5.0 (Mobile; rv:109.0) Gecko/109.0 Firefox/119.0",
        "Mozilla/5.0 (Mobile; rv:108.0) Gecko/108.0 Firefox/118.0",
        
        // Android Samsung Internet
        "Mozilla/5.0 (Linux; Android 13; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/23.0 Chrome/115.0.0.0 Mobile Safari/537.36",
        "Mozilla/5.0 (Linux; Android 12; SM-G998B) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/22.0 Chrome/111.0.0.0 Mobile Safari/537.36"
    };

    private static readonly string[] _platforms = {
        "Win32", "MacIntel", "Linux x86_64", "Linux i686", "Linux armv7l", "Linux aarch64"
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

    // Desktop viewport sizes
    private static readonly ViewportSize[] _desktopViewportSizes = {
        new ViewportSize { Width = 1920, Height = 1080 },
        new ViewportSize { Width = 1366, Height = 768 },
        new ViewportSize { Width = 1440, Height = 900 },
        new ViewportSize { Width = 1536, Height = 864 },
        new ViewportSize { Width = 1280, Height = 720 },
        new ViewportSize { Width = 1600, Height = 900 }
    };

    // Mobile viewport sizes (Android)
    private static readonly ViewportSize[] _mobileViewportSizes = {
        new ViewportSize { Width = 393, Height = 851 }, // Pixel 7
        new ViewportSize { Width = 412, Height = 915 }, // Pixel 6
        new ViewportSize { Width = 360, Height = 800 }, // Galaxy S21
        new ViewportSize { Width = 384, Height = 854 }, // Galaxy S22
        new ViewportSize { Width = 412, Height = 869 }, // OnePlus
        new ViewportSize { Width = 375, Height = 812 }, // Common mobile size
        new ViewportSize { Width = 414, Height = 896 }, // Large mobile
        new ViewportSize { Width = 390, Height = 844 }  // Medium mobile
    };

    private static readonly string[] _desktopScreenResolutions = {
        "1920x1080", "1366x768", "1440x900", "1536x864", "1280x720", "1600x900"
    };

    private static readonly string[] _mobileScreenResolutions = {
        "393x851", "412x915", "360x800", "384x854", "412x869", "375x812", "414x896", "390x844"
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
        public bool IsMobile { get; set; }
        public string DeviceType { get; set; } // "desktop", "mobile"
    }

    public static FingerprintProfile GenerateRandomFingerprint(int instanceId)
    {
        var userAgent = _userAgents[_random.Next(_userAgents.Length)];
        var isMobile = IsMobileUserAgent(userAgent);
        
        var viewport = isMobile 
            ? _mobileViewportSizes[_random.Next(_mobileViewportSizes.Length)]
            : _desktopViewportSizes[_random.Next(_desktopViewportSizes.Length)];
            
        var screenRes = isMobile
            ? _mobileScreenResolutions[_random.Next(_mobileScreenResolutions.Length)]
            : _desktopScreenResolutions[_random.Next(_desktopScreenResolutions.Length)];

        var platform = GetPlatformForUserAgent(userAgent);
        var devicePixelRatio = isMobile ? GetMobileDevicePixelRatio() : GetDesktopDevicePixelRatio();

        return new FingerprintProfile
        {
            UserAgent = userAgent,
            Platform = platform,
         //   Language = _languages[_random.Next(_languages.Length)],
         //   Timezone = _timezones[_random.Next(_timezones.Length)],
            ViewportSize = viewport,
            ScreenResolution = screenRes,
            ColorDepth = isMobile ? GetMobileColorDepth() : GetDesktopColorDepth(),
            DevicePixelRatio = devicePixelRatio,
            CookiesEnabled = true,
            DoNotTrack = _random.Next(2) == 0,
            ExtraHeaders = GenerateRandomHeaders(isMobile),
            BrowserArgs = GenerateRandomBrowserArgs(instanceId, isMobile),
            IsMobile = isMobile,
            DeviceType = isMobile ? "mobile" : "desktop"
        };
    }

    private static bool IsMobileUserAgent(string userAgent)
    {
        return userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("SamsungBrowser");
    }

    private static string GetPlatformForUserAgent(string userAgent)
    {
        if (userAgent.Contains("Android"))
            return _random.Next(2) == 0 ? "Linux armv7l" : "Linux aarch64";
        
        if (userAgent.Contains("Windows"))
            return "Win32";
            
        if (userAgent.Contains("Macintosh"))
            return "MacIntel";
            
        if (userAgent.Contains("Linux"))
            return _random.Next(2) == 0 ? "Linux x86_64" : "Linux i686";
            
        return _platforms[_random.Next(_platforms.Length)];
    }

    private static int GetMobileDevicePixelRatio()
    {
        var mobileRatios = new[] { 2, 3, 2.5, 2.75, 3.5 };
        return (int)(mobileRatios[_random.Next(mobileRatios.Length)] * 100) / 100; // Convert to int for simplicity
    }

    private static int GetDesktopDevicePixelRatio()
    {
        return _random.Next(2) == 0 ? 1 : 2;
    }

    private static int GetMobileColorDepth()
    {
        return _random.Next(2) == 0 ? 24 : 32; // Mobile devices typically have 24 or 32
    }

    private static int GetDesktopColorDepth()
    {
        return _random.Next(2) == 0 ? 24 : 32;
    }

    private static Dictionary<string, string> GenerateRandomHeaders(bool isMobile)
    {
        var headers = new Dictionary<string, string>
        {
            ["sec-ch-ua"] = GenerateSecChUa(isMobile),
            ["sec-ch-ua-mobile"] = isMobile ? "?1" : "?0",
            ["sec-ch-ua-platform"] = $"\"{GetRandomPlatformForHeader(isMobile)}\"",
            ["sec-fetch-dest"] = "document",
            ["sec-fetch-mode"] = "navigate",
            ["sec-fetch-site"] = "none",
            ["sec-fetch-user"] = "?1",
            ["upgrade-insecure-requests"] = "1"
        };

        // Add mobile-specific headers
        if (isMobile)
        {
            headers["sec-ch-ua-arch"] = "\"arm\"";
            headers["sec-ch-ua-bitness"] = "\"64\"";
            headers["sec-ch-ua-full-version-list"] = GenerateMobileFullVersionList();
        }

        // Randomly add some optional headers
        if (_random.Next(2) == 0)
        {
            headers["dnt"] = "1";
        }

        return headers;
    }

    private static string GenerateSecChUa(bool isMobile)
    {
        if (isMobile)
        {
            var mobileBrands = new[]
            {
                "\"Google Chrome\";v=\"119\", \"Chromium\";v=\"119\", \"Not?A_Brand\";v=\"24\"",
                "\"Google Chrome\";v=\"118\", \"Chromium\";v=\"118\", \"Not=A?Brand\";v=\"99\"",
                "\"Samsung Internet\";v=\"23\", \"Not.A/Brand\";v=\"8\", \"Chromium\";v=\"115\"",
                "\"Mobile Safari\";v=\"17\", \"WebKit\";v=\"605\""
            };
            return mobileBrands[_random.Next(mobileBrands.Length)];
        }
        else
        {
            var desktopBrands = new[]
            {
                "\"Google Chrome\";v=\"119\", \"Chromium\";v=\"119\", \"Not?A_Brand\";v=\"24\"",
                "\"Google Chrome\";v=\"118\", \"Chromium\";v=\"118\", \"Not=A?Brand\";v=\"99\"",
                "\"Chromium\";v=\"119\", \"Not?A_Brand\";v=\"24\"",
                "\"Google Chrome\";v=\"119\", \"Not;A=Brand\";v=\"24\""
            };
            return desktopBrands[_random.Next(desktopBrands.Length)];
        }
    }

    private static string GetRandomPlatformForHeader(bool isMobile)
    {
        if (isMobile)
        {
            return "Android";
        }
        else
        {
            var platforms = new[] { "Windows", "macOS", "Linux" };
            return platforms[_random.Next(platforms.Length)];
        }
    }

    private static string GenerateMobileFullVersionList()
    {
        var versions = new[]
        {
            "\"Google Chrome\";v=\"119.0.6045.163\", \"Chromium\";v=\"119.0.6045.163\", \"Not?A_Brand\";v=\"24.0.0.0\"",
            "\"Google Chrome\";v=\"118.0.5993.117\", \"Chromium\";v=\"118.0.5993.117\", \"Not=A?Brand\";v=\"99.0.0.0\"",
            "\"Samsung Internet\";v=\"23.0.1.1\", \"Not.A/Brand\";v=\"8.0.0.0\", \"Chromium\";v=\"115.0.5790.136\""
        };
        return versions[_random.Next(versions.Length)];
    }

    private static string[] GenerateRandomBrowserArgs(int instanceId, bool isMobile)
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

        // Mobile-specific args
        if (isMobile)
        {
            baseArgs.AddRange(new[]
            {
                "--use-mobile-user-agent",
                "--enable-features=UseOzonePlatform",
                "--ozone-platform=wayland",
                "--force-device-scale-factor=2",
                "--simulate-outdated-no-au='Tue, 31 Dec 2099 23:59:59 GMT'",
                "--disable-features=TranslateUI",
                "--disable-ipc-flooding-protection"
            });
        }

        // Add random canvas and webgl fingerprint modifications
        if (_random.Next(2) == 0)
        {
            baseArgs.Add("--disable-reading-from-canvas");
        }

        if (_random.Next(2) == 0)
        {
            baseArgs.Add("--disable-webgl");
        }

        // Random memory and CPU related args (adjusted for mobile)
        if (isMobile)
        {
            var mobileMemoryArgs = new[]
            {
                "--max_old_space_size=1024",
                "--max_old_space_size=2048",
                "--max_old_space_size=1536"
            };
            baseArgs.Add(mobileMemoryArgs[_random.Next(mobileMemoryArgs.Length)]);
        }
        else
        {
            var desktopMemoryArgs = new[]
            {
                "--max_old_space_size=4096",
                "--max_old_space_size=2048",
                "--max_old_space_size=8192"
            };
            baseArgs.Add(desktopMemoryArgs[_random.Next(desktopMemoryArgs.Length)]);
        }

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

            // Mobile-specific navigator properties
            {(profile.IsMobile ? $@"
            Object.defineProperty(navigator, 'maxTouchPoints', {{
                get: () => {_random.Next(1, 11)}
            }});
            
            Object.defineProperty(navigator, 'vendor', {{
                get: () => 'Google Inc.'
            }});
            
            Object.defineProperty(navigator, 'appVersion', {{
                get: () => '{profile.UserAgent.Substring(profile.UserAgent.IndexOf("(") + 1).Replace(")", "")}'
            }});
            " : "")}

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

            {(profile.IsMobile ? @"
            // Mobile-specific screen properties
            Object.defineProperty(screen, 'orientation', {
                get: () => ({
                    angle: 0,
                    type: 'portrait-primary'
                })
            });
            
            // Touch events support
            window.TouchEvent = window.TouchEvent || function TouchEvent() {};
            " : "")}

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
                    return '{(profile.IsMobile ? "Qualcomm" : "Intel Inc.")}';
                }}
                if (parameter === 37446) {{ // UNMASKED_RENDERER_WEBGL
                    {(profile.IsMobile ? @"
                    const mobileRenderers = ['Adreno (TM) 730', 'Adreno (TM) 640', 'Mali-G78 MP14', 'Mali-G77 MP11'];
                    return mobileRenderers[Math.floor(Math.random() * mobileRenderers.length)];" : @"
                    const desktopRenderers = ['Intel Iris OpenGL Engine', 'ANGLE (Intel, Intel(R) HD Graphics 630 Direct3D11 vs_5_0 ps_5_0)', 'NVIDIA GeForce GTX 1060'];
                    return desktopRenderers[Math.floor(Math.random() * desktopRenderers.length)];")}
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
            DeviceScaleFactor = profile.DevicePixelRatio,
            IsMobile = profile.IsMobile,
            HasTouch = profile.IsMobile
        };

        if (proxy != null)
        {
            options.Proxy = proxy;
        }

        return options;
    }
}