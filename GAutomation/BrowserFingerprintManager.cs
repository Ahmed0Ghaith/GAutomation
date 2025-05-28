using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.Json;

public class BrowserFingerprintManager
{
    private static readonly Random _random = new Random();

    // Extensive user agent database with more recent versions
    private static readonly string[] _userAgents = {
        // Chrome Windows
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36",
        
        // Chrome macOS
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        
        // Chrome Linux
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
        
        // Firefox
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:120.0) Gecko/20100101 Firefox/120.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:119.0) Gecko/20100101 Firefox/119.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:120.0) Gecko/20100101 Firefox/120.0",
        "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:120.0) Gecko/20100101 Firefox/120.0",
        
        // Edge
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36 Edg/119.0.0.0"
    };

    private static readonly string[] _platforms = {
        "Win32", "MacIntel", "Linux x86_64"
    };

    //private static readonly string[] _languages = {
    //    "en-US,en;q=0.9",
    //    "en-GB,en;q=0.9",
    //    "en-CA,en;q=0.9,fr;q=0.8",
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
    //    "America/Toronto",
    //    "America/Denver",
    //    "Europe/Madrid"
    //};

    private static readonly ViewportSize[] _viewportSizes = {
        new ViewportSize { Width = 1920, Height = 1080 },
        new ViewportSize { Width = 1366, Height = 768 },
        new ViewportSize { Width = 1440, Height = 900 },
        new ViewportSize { Width = 1536, Height = 864 },
        new ViewportSize { Width = 1280, Height = 720 },
        new ViewportSize { Width = 1600, Height = 900 },
        new ViewportSize { Width = 1680, Height = 1050 },
        new ViewportSize { Width = 1280, Height = 1024 },
        new ViewportSize { Width = 1024, Height = 768 }
    };

    private static readonly string[] _screenResolutions = {
        "1920x1080", "1366x768", "1440x900", "1536x864", "1280x720",
        "1600x900", "1680x1050", "1280x1024", "1024x768"
    };

    public class FingerprintProfile
    {
        public string UserAgent { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
      //  public string Language { get; set; } = string.Empty;
     ///   public string Timezone { get; set; } = string.Empty;
        public ViewportSize ViewportSize { get; set; } = new ViewportSize();
        public string ScreenResolution { get; set; } = string.Empty;
        public int ColorDepth { get; set; }
        public float DevicePixelRatio { get; set; }
        public bool CookiesEnabled { get; set; }
        public bool DoNotTrack { get; set; }
        public Dictionary<string, string> ExtraHeaders { get; set; } = new Dictionary<string, string>();
        public string[] BrowserArgs { get; set; } = Array.Empty<string>();
        public int SlowMo { get; set; }
        public int HardwareConcurrency { get; set; }
        public string WebGLVendor { get; set; } = string.Empty;
        public string WebGLRenderer { get; set; } = string.Empty;
        public Dictionary<string, object> MemoryInfo { get; set; } = new Dictionary<string, object>();
    }

    public static FingerprintProfile GenerateRandomFingerprint(int instanceId)
    {
        var userAgent = _userAgents[_random.Next(_userAgents.Length)];
        var viewport = _viewportSizes[_random.Next(_viewportSizes.Length)];
        var screenRes = _screenResolutions[_random.Next(_screenResolutions.Length)];

        // Generate realistic hardware specs
        var hardwareConcurrency = new[] { 2, 4, 6, 8, 12, 16 }[_random.Next(6)];

        // WebGL vendor/renderer combinations
        var webglCombos = new[]
        {
            ("Intel Inc.", "Intel Iris OpenGL Engine"),
            ("Intel Inc.", "Intel(R) UHD Graphics 630"),
            ("NVIDIA Corporation", "NVIDIA GeForceRTX 3060/PCIe/SSE2"),
            ("NVIDIA Corporation", "NVIDIA GeForce GTX 1660/PCIe/SSE2"),
            ("AMD", "AMD Radeon RX 580 Series"),
            ("Intel Inc.", "ANGLE (Intel, Intel(R) HD Graphics 4000 Direct3D11 vs_5_0 ps_5_0)")
        };
        var webglCombo = webglCombos[_random.Next(webglCombos.Length)];

        return new FingerprintProfile
        {
            UserAgent = userAgent,
            Platform = _platforms[_random.Next(_platforms.Length)],
           // Language = _languages[_random.Next(_languages.Length)],
          //  Timezone = _timezones[_random.Next(_timezones.Length)],
            ViewportSize = viewport,
            ScreenResolution = screenRes,
            ColorDepth = _random.Next(2) == 0 ? 24 : 32,
            DevicePixelRatio = _random.NextSingle() * 0.5f + 1.0f, // 1.0 to 1.5
            CookiesEnabled = true,
            DoNotTrack = _random.Next(3) == 0, // 33% chance
            ExtraHeaders = GenerateRandomHeaders(),
            BrowserArgs = GenerateRandomBrowserArgs(instanceId),
            SlowMo = _random.Next(30, 120), // Random slow motion between 30-120ms
            HardwareConcurrency = hardwareConcurrency,
            WebGLVendor = webglCombo.Item1,
            WebGLRenderer = webglCombo.Item2,
            MemoryInfo = GenerateMemoryInfo()
        };
    }

    private static Dictionary<string, object> GenerateMemoryInfo()
    {
        var baseMemory = _random.Next(20000000, 80000000);
        return new Dictionary<string, object>
        {
            ["usedJSHeapSize"] = baseMemory + _random.Next(-5000000, 15000000),
            ["totalJSHeapSize"] = baseMemory + _random.Next(20000000, 50000000),
            ["jsHeapSizeLimit"] = 2172649472 + _random.Next(-100000000, 100000000)
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
            ["upgrade-insecure-requests"] = "1",
            ["cache-control"] = _random.Next(2) == 0 ? "max-age=0" : "no-cache"
        };

        // Randomly add some optional headers
        if (_random.Next(3) == 0)
        {
            headers["dnt"] = "1";
        }

        if (_random.Next(4) == 0)
        {
            headers["pragma"] = "no-cache";
        }

        // Add Accept-Language with slight variations
        var acceptLang = new[]
        {
            "en-US,en;q=0.9",
            "en-GB,en;q=0.9",
            "en-US,en;q=0.9,es;q=0.8",
            "en-GB,en;q=0.9,fr;q=0.8"
        };
        headers["accept-language"] = acceptLang[_random.Next(acceptLang.Length)];

        return headers;
    }

    private static string GenerateSecChUa()
    {
        var brands = new[]
        {
            "\"Google Chrome\";v=\"120\", \"Chromium\";v=\"120\", \"Not_A Brand\";v=\"24\"",
            "\"Google Chrome\";v=\"119\", \"Chromium\";v=\"119\", \"Not?A_Brand\";v=\"24\"",
            "\"Google Chrome\";v=\"118\", \"Chromium\";v=\"118\", \"Not=A?Brand\";v=\"99\"",
            "\"Chromium\";v=\"120\", \"Not_A Brand\";v=\"24\"",
            "\"Microsoft Edge\";v=\"120\", \"Chromium\";v=\"120\", \"Not_A Brand\";v=\"24\""
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
            "--disable-dev-shm-usage",
            "--disable-background-timer-throttling",
            "--disable-backgrounding-occluded-windows",
            "--disable-renderer-backgrounding",
            "--disable-field-trial-config",
            "--disable-back-forward-cache",
            "--disable-ipc-flooding-protection",
            $"--window-position={instanceId * 150 + _random.Next(100)},{instanceId * 80 + _random.Next(50)}"
        };

        // Randomly add performance and fingerprinting related args
        var optionalArgs = new[]
        {
            "--disable-canvas-aa",
            "--disable-2d-canvas-clip-aa",
            "--disable-gl-drawing-for-tests",
            "--disable-accelerated-2d-canvas",
            "--no-sandbox",
            "--disable-setuid-sandbox",
            "--disable-background-networking",
            "--disable-default-apps",
            "--disable-sync",
            "--disable-translate",
            "--hide-scrollbars",
            "--metrics-recording-only",
            "--mute-audio",
            "--no-first-run",
            "--safebrowsing-disable-auto-update",
            "--ignore-ssl-errors",
            "--ignore-certificate-errors",
            "--allow-running-insecure-content",
            "--disable-component-update"
        };

        // Add 30-60% of optional args randomly
        var numOptional = _random.Next(optionalArgs.Length * 3 / 10, optionalArgs.Length * 6 / 10);
        var selectedOptional = optionalArgs.OrderBy(x => _random.Next()).Take(numOptional);
        baseArgs.AddRange(selectedOptional);

        // Add random user data directory
        if (_random.Next(2) == 0)
        {
            baseArgs.Add($"--user-data-dir=/tmp/chrome-{instanceId}-{_random.Next(10000)}");
        }

        // Random memory and CPU related args
        var memoryArgs = new[]
        {
            "--max_old_space_size=4096",
            "--max_old_space_size=2048",
            "--max_old_space_size=8192",
            "--max_old_space_size=1024"
        };
        baseArgs.Add(memoryArgs[_random.Next(memoryArgs.Length)]);

        return baseArgs.ToArray();
    }

    public static async Task ApplyFingerprintToContext(IBrowserContext context, FingerprintProfile profile)
    {
        // Create JavaScript injection script with proper escaping
       // var languageArray = JsonSerializer.Serialize(profile.Language.Split(',').Select(l => l.Split(';')[0].Trim()).ToArray());
        var userAgentEscaped = profile.UserAgent.Replace("\\", "\\\\").Replace("'", "\\'");
        var webglVendorEscaped = profile.WebGLVendor.Replace("\\", "\\\\").Replace("'", "\\'");
        var webglRendererEscaped = profile.WebGLRenderer.Replace("\\", "\\\\").Replace("'", "\\'");
        var platformOs = profile.Platform.ToLower().Contains("win") ? "win" :
                        (profile.Platform.ToLower().Contains("mac") ? "mac" : "linux");
        var devicePixelRatioStr = profile.DevicePixelRatio.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var randomSeed = _random.Next(1000000);

        var script = $@"
// Remove all webdriver traces
delete navigator.__proto__.webdriver;
delete navigator.webdriver;
delete window.navigator.webdriver;

// Override webdriver property completely
Object.defineProperty(navigator, 'webdriver', {{
    get: () => undefined,
    configurable: true
}});

// Override navigator properties with fingerprint data
Object.defineProperty(navigator, 'userAgent', {{
    get: () => '{userAgentEscaped}',
    configurable: true
}});

Object.defineProperty(navigator, 'platform', {{
    get: () => '{profile.Platform}',
    configurable: true
}});



Object.defineProperty(navigator, 'cookieEnabled', {{
    get: () => {profile.CookiesEnabled.ToString().ToLower()},
    configurable: true
}});

Object.defineProperty(navigator, 'doNotTrack', {{
    get: () => '{(profile.DoNotTrack ? "1" : "0")}',
    configurable: true
}});

Object.defineProperty(navigator, 'hardwareConcurrency', {{
    get: () => {profile.HardwareConcurrency},
    configurable: true
}});

// Override screen properties
Object.defineProperty(screen, 'width', {{
    get: () => {profile.ViewportSize.Width},
    configurable: true
}});

Object.defineProperty(screen, 'height', {{
    get: () => {profile.ViewportSize.Height},
    configurable: true
}});

Object.defineProperty(screen, 'availWidth', {{
    get: () => {profile.ViewportSize.Width},
    configurable: true
}});

Object.defineProperty(screen, 'availHeight', {{
    get: () => {profile.ViewportSize.Height - 40},
    configurable: true
}});

Object.defineProperty(screen, 'colorDepth', {{
    get: () => {profile.ColorDepth},
    configurable: true
}});

Object.defineProperty(screen, 'pixelDepth', {{
    get: () => {profile.ColorDepth},
    configurable: true
}});

Object.defineProperty(window, 'devicePixelRatio', {{
    get: () => {devicePixelRatioStr},
    configurable: true
}});

// Mock realistic plugins
Object.defineProperty(navigator, 'plugins', {{
    get: () => [
        {{
            0: {{ type: 'application/x-google-chrome-pdf', suffixes: 'pdf', description: 'Portable Document Format' }},
            description: 'Portable Document Format',
            filename: 'internal-pdf-viewer',
            length: 1,
            name: 'Chrome PDF Plugin'
        }},
        {{
            0: {{ type: 'application/x-nacl', suffixes: 'nexe', description: 'Native Client Executable' }},
            description: 'Native Client',
            filename: 'internal-nacl-plugin',
            length: 1,
            name: 'Native Client'
        }},
        {{
            0: {{ type: 'application/x-ppapi-widevine-cdm', suffixes: '', description: 'Widevine Content Decryption Module' }},
            description: 'Widevine Content Decryption Module',
            filename: 'widevinecdmadapter.dll',
            length: 1,
            name: 'Widevine Content Decryption Module'
        }}
    ],
    configurable: true
}});

// Enhanced Chrome runtime mock
if (!window.chrome) {{
    window.chrome = {{}};
}}
window.chrome.runtime = {{
    onConnect: undefined,
    onMessage: undefined,
    PlatformOs: '{platformOs}'
}};

// Mock connection properties with realistic values
Object.defineProperty(navigator, 'connection', {{
    get: () => ({{
        effectiveType: ['4g', '3g', 'slow-2g'][Math.floor(Math.random() * 3)],
        rtt: Math.floor(Math.random() * 100) + 50,
        downlink: Math.random() * 10 + 1,
        saveData: false
    }}),
    configurable: true
}});

// Mock permissions API
const originalQuery = navigator.permissions?.query;
if (navigator.permissions) {{
    navigator.permissions.query = (parameters) => {{
        if (parameters.name === 'notifications') {{
            return Promise.resolve({{ state: 'default' }});
        }}
        return originalQuery ? originalQuery(parameters) : Promise.resolve({{ state: 'granted' }});
    }};
}}

// Enhanced canvas fingerprint randomization
const originalToDataURL = HTMLCanvasElement.prototype.toDataURL;
const originalGetImageData = CanvasRenderingContext2D.prototype.getImageData;

HTMLCanvasElement.prototype.toDataURL = function(format) {{
    const context = this.getContext('2d');
    if (context) {{
        const imageData = context.getImageData(0, 0, this.width, this.height);
        const data = imageData.data;

        // Add consistent but subtle noise
        for (let i = 0; i < data.length; i += 4) {{
            const noise = (Math.sin(i * 0.01) + Math.cos(i * 0.02)) * 2;
            data[i] = Math.min(255, Math.max(0, data[i] + noise));
            data[i + 1] = Math.min(255, Math.max(0, data[i + 1] + noise * 0.7));
            data[i + 2] = Math.min(255, Math.max(0, data[i + 2] + noise * 1.3));
        }}

        context.putImageData(imageData, 0, 0);
    }}
    return originalToDataURL.apply(this, arguments);
}};

// Enhanced WebGL fingerprint spoofing
const getParameter = WebGLRenderingContext.prototype.getParameter;
WebGLRenderingContext.prototype.getParameter = function(parameter) {{
    if (parameter === 37445) {{ // UNMASKED_VENDOR_WEBGL
        return '{webglVendorEscaped}';
    }}
    if (parameter === 37446) {{ // UNMASKED_RENDERER_WEBGL
        return '{webglRendererEscaped}';
    }}
    if (parameter === 34047) {{ // MAX_VERTEX_ATTRIBS
        return 16 + Math.floor(Math.random() * 16);
    }}
    return getParameter.apply(this, arguments);
}};

// Mock performance memory with realistic values
if (performance.memory) {{
    Object.defineProperty(performance, 'memory', {{
        get: () => ({{
            usedJSHeapSize: {profile.MemoryInfo["usedJSHeapSize"]},
            totalJSHeapSize: {profile.MemoryInfo["totalJSHeapSize"]},
            jsHeapSizeLimit: {profile.MemoryInfo["jsHeapSizeLimit"]}
        }}),
        configurable: true
    }});
}}




// Add subtle randomization to Math.random
const originalRandom = Math.random;
let seed = {randomSeed};
Math.random = function() {{
    seed = (seed * 9301 + 49297) % 233280;
    return seed / 233280;
}};

// Override getBoundingClientRect with slight variations
const originalGetBoundingClientRect = Element.prototype.getBoundingClientRect;
Element.prototype.getBoundingClientRect = function() {{
    const rect = originalGetBoundingClientRect.call(this);
    const noise = () => (Math.random() - 0.5) * 0.2;
    return {{
        x: rect.x + noise(),
        y: rect.y + noise(),
        width: rect.width + noise(),
        height: rect.height + noise(),
        top: rect.top + noise(),
        right: rect.right + noise(),
        bottom: rect.bottom + noise(),
        left: rect.left + noise(),
        toJSON: rect.toJSON
    }};
}};

// Mock battery API (if available)
if (navigator.getBattery) {{
    const originalGetBattery = navigator.getBattery;
    navigator.getBattery = async function() {{
        const battery = await originalGetBattery.call(this);
        return {{
            ...battery,
            level: 0.5 + Math.random() * 0.4,
            charging: Math.random() > 0.5,
            chargingTime: Math.random() * 7200,
            dischargingTime: Math.random() * 14400
        }};
    }};
}}

// Add noise to audio context fingerprinting
if (window.AudioContext || window.webkitAudioContext) {{
    const OriginalAudioContext = window.AudioContext || window.webkitAudioContext;
    const audioContext = new OriginalAudioContext();
    const originalCreateOscillator = audioContext.createOscillator;

    audioContext.createOscillator = function() {{
        const oscillator = originalCreateOscillator.call(this);
        const originalFrequency = oscillator.frequency.value;
        oscillator.frequency.value = originalFrequency + (Math.random() - 0.5) * 0.1;
        return oscillator;
    }};
}}

console.log('🔒 Advanced fingerprint protection loaded');
";

        await context.AddInitScriptAsync(script);
    }

    public static BrowserNewContextOptions CreateContextOptions(FingerprintProfile profile, Proxy? proxy = null)
    {
        var options = new BrowserNewContextOptions
        {
            UserAgent = profile.UserAgent,
            ViewportSize = profile.ViewportSize,
            ExtraHTTPHeaders = profile.ExtraHeaders,
          ////  Locale = profile.Language.Split(',')[0],
          ///  TimezoneId = profile.Timezone,
            DeviceScaleFactor = profile.DevicePixelRatio,
            IsMobile = false,
            HasTouch = false,
            ColorScheme = _random.Next(2) == 0 ? ColorScheme.Light : ColorScheme.Dark
        };

        if (proxy != null)
        {
            options.Proxy = proxy;
        }

        return options;
    }
}