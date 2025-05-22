using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ProxyManager
{
    private static readonly ConcurrentQueue<ProxyInfo> _availableProxies = new ConcurrentQueue<ProxyInfo>();
    private static readonly ConcurrentDictionary<int, ProxyInfo> _usedProxies = new ConcurrentDictionary<int, ProxyInfo>();
    private static readonly object _lockObject = new object();
    private static bool _isInitialized = false;

    public class ProxyInfo
    {
        public string Hostname { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullAddress => $"http://{Hostname}:{Port}";

        public override string ToString()
        {
            return $"{Hostname}:{Port}:{Username}:{Password}";
        }
    }

    /// <summary>
    /// Load proxies from a text file with format: hostname:port:username:password
    /// </summary>
    /// <param name="filePath">Path to the proxy file</param>
    public static void LoadProxiesFromFile(string filePath = "proxies.txt")
    {
        if (_isInitialized)
            return;

        lock (_lockObject)
        {
            if (_isInitialized)
                return;

            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Proxy file '{filePath}' not found. Creating sample file...");
                    CreateSampleProxyFile(filePath);
                    return;
                }

                var lines = File.ReadAllLines(filePath);
                int loadedCount = 0;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var parts = line.Trim().Split(':');
                    if (parts.Length >= 4)
                    {
                        var proxy = new ProxyInfo
                        {
                            Hostname = parts[0].Trim(),
                            Port = parts[1].Trim(),
                            Username = parts[2].Trim(),
                            Password = string.Join(":", parts.Skip(3)).Trim() // Handle passwords with colons
                        };

                        _availableProxies.Enqueue(proxy);
                        loadedCount++;
                    }
                    else
                    {
                        Console.WriteLine($"Invalid proxy format: {line}");
                    }
                }

                Console.WriteLine($"Loaded {loadedCount} proxies from {filePath}");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading proxies from file: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Get a unique proxy for an instance
    /// </summary>
    /// <param name="instanceId">The instance ID</param>
    /// <returns>ProxyInfo or null if no proxy available</returns>
    public static ProxyInfo GetProxyForInstance(int instanceId)
    {
        // Check if this instance already has a proxy assigned
        if( _usedProxies.TryGetValue(instanceId, out var existingProxy))
        {
            return existingProxy;
        }

        // Try to get a new proxy
        if (_availableProxies.TryDequeue(out var proxy))
        {
            _usedProxies[instanceId] = proxy;
            Console.WriteLine($"Instance {instanceId}: Assigned proxy {proxy.Hostname}:{proxy.Port}");
            return proxy;
        }

        Console.WriteLine($"Instance {instanceId}: No available proxies remaining");
        return null;
    }

    /// <summary>
    /// Release a proxy when instance is done (makes it available again if needed)
    /// </summary>
    /// <param name="instanceId">The instance ID</param>
    public static void ReleaseProxy(int instanceId)
    {
        if (_usedProxies.TryRemove(instanceId, out var proxy))
        {
            // Optionally, you can add it back to available proxies if you want to reuse
            // _availableProxies.Enqueue(proxy);
            Console.WriteLine($"Instance {instanceId}: Released proxy {proxy.Hostname}:{proxy.Port}");
        }
    }

    /// <summary>
    /// Get the count of available proxies
    /// </summary>
    public static int AvailableProxyCount => _availableProxies.Count;

    /// <summary>
    /// Get the count of used proxies
    /// </summary>
    public static int UsedProxyCount => _usedProxies.Count;

    /// <summary>
    /// Create a sample proxy file for reference
    /// </summary>
    private static void CreateSampleProxyFile(string filePath)
    {
        try
        {
            var sampleContent = @"# Proxy file format: hostname:port:username:password
# Lines starting with # are comments and will be ignored
# Example proxies (replace with your actual proxies):

proxy1.example.com:8080:user1:pass1
proxy2.example.com:8080:user2:pass2
proxy3.example.com:8080:user3:pass3
192.168.1.100:3128:username:password
10.0.0.1:8888:admin:admin123

# You can add as many proxies as needed
# Make sure each line follows the exact format: hostname:port:username:password";

            File.WriteAllText(filePath, sampleContent);
            Console.WriteLine($"Sample proxy file created at '{filePath}'. Please edit it with your actual proxies.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating sample proxy file: {ex.Message}");
        }
    }

    /// <summary>
    /// Display proxy statistics
    /// </summary>
    public static void DisplayStats()
    {
        Console.WriteLine($"Proxy Statistics:");
        Console.WriteLine($"- Available: {AvailableProxyCount}");
        Console.WriteLine($"- In Use: {UsedProxyCount}");
        Console.WriteLine($"- Total Loaded: {AvailableProxyCount + UsedProxyCount}");
    }
}