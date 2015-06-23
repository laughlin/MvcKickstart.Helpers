using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.CacheAccess;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace MvcKickstart.Infrastructure
{
    public class MemoryCacheClient : ICacheClient, ICacheClientBroadcaster
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MemoryCacheClient));

        private ConcurrentDictionary<string, CacheEntry> _memory;
        private ConcurrentDictionary<string, int> _counters;
        public readonly IList<string> BroadcastNodes;

        public bool FlushOnDispose { get; set; }

        private class CacheEntry
        {
            private object _cacheValue;

            public CacheEntry(object value, DateTime expiresAt)
            {
                Value = value;
                ExpiresAt = expiresAt;
                LastModifiedTicks = DateTime.Now.Ticks;
            }

            internal DateTime ExpiresAt { get; set; }

            internal object Value
            {
                get { return _cacheValue; }
                set
                {
                    _cacheValue = value;
                    LastModifiedTicks = DateTime.Now.Ticks;
                }
            }

            internal long LastModifiedTicks { get; private set; }
        }

        public MemoryCacheClient()
        {
            _memory = new ConcurrentDictionary<string, CacheEntry>();
            _counters = new ConcurrentDictionary<string, int>();
            BroadcastNodes = new List<string>();
            try
            {
                var broadcastConfigNodes =
                    (ConfigurationManager.AppSettings["CacheStack:BroadcastNodes"] ?? string.Empty).Split(new[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries);
                ConfigureCache(broadcastConfigNodes);
            }
            catch (Exception ex)
            {
                // We don't want to disable the entire app if the cluster caching invalidation doesn't init properly. Just log and continue
                Log.Error("Error parsing cache broadcast node information", ex);
            }
        }

        public bool ConfigureCache(IEnumerable<string> broadcastConfigNodes)
        {
            var result = true;
            // Expected a list of machine names like bia-web1,bia-web2
            if (broadcastConfigNodes == null)
            {
                return false;
            }

            foreach (var broadcastConfigNode in broadcastConfigNodes)
            {
                if (!HandleBroadcastConfigurationNode(broadcastConfigNode))
                {
                    result = false;
                }
            }
            return result;
        }

        public bool HandleBroadcastConfigurationNode(string broadcastConfigNode)
        {
            if (string.IsNullOrEmpty(broadcastConfigNode))
            {
                return false;
            }
            if (broadcastConfigNode.Contains(':'))
            {
                Log.InfoFormat("Parsing cache broadcast node map: {0}", broadcastConfigNode);
                //NOTE: if we do not include empty entries we are asking for accidental configuraion slipups
                var mapParts = broadcastConfigNode.Split(new[] { ':' }, StringSplitOptions.None);
                switch (mapParts.Length)
                {
                    //Traditional map <machinename>:<url>
                    case 2:
                        return MapNode(mapParts, broadcastConfigNode);
                    //With Options map <machinename>:<url>:<options>
                    //Options in separated by - eg:  -force-ignorelocal => force mapping, ignore local setting
                    case 3:
                        var options = mapParts[2].Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                        Log.InfoFormat("broadcast mapping with options: []", mapParts.Join(","));
                        var overrideMachineNameFilter = options.Contains("force", StringComparer.OrdinalIgnoreCase) ||
                                                         options.Contains("ignorelocal", StringComparer.OrdinalIgnoreCase);
                        return MapNode(mapParts, broadcastConfigNode, overrideMachineNameFilter);
                    default:
                        Log.Warn(
                            string.Format(
                                "BroadCast Map format incorrect. Expected <computer-name>:<url> or <computer-name>:<url>:<options>. Instead received: {0}",
                                broadcastConfigNode));
                        break;
                }
            }
            else
            {
                Log.InfoFormat("Parsing cache broadcast node: {0}", broadcastConfigNode);
                // Don't broadcast to yourself
                if (Environment.MachineName.Equals(broadcastConfigNode, StringComparison.OrdinalIgnoreCase))
                {
                    Log.InfoFormat("Ignoring local machine cache broadcast node: ", broadcastConfigNode);
                }
                else
                {
                    return AddNodeToBroadcastMap(broadcastConfigNode);
                }
            }
            return false;
        }

        public bool MapNode(string[] mapParts, string broadcastConfigNode, bool ignoreLocalMachine = false)
        {
            if (mapParts.Length < 2)
            {
                return false;
            }
            if (Environment.MachineName.Equals(mapParts[0], StringComparison.OrdinalIgnoreCase))
            {
                if (ignoreLocalMachine)
                {
                    Log.InfoFormat("Including Local machine broadcast node: {0}", broadcastConfigNode);
                }
                else
                {
                    Log.InfoFormat("Ignoring local machine cache broadcast node: {0}", broadcastConfigNode);
                    return false;
                }
            }
            if (!AddNodeToBroadcastMap(mapParts[1]))
            {
                Log.InfoFormat("Not adding element to broadcast node map: [{0}|{1}]", mapParts[0], mapParts[1]);
                return false;
            }
            return true;
        }

        public bool AddNodeToBroadcastMap(string nodeName)
        {
            var url = ConvertToUrl(nodeName);
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }
            Log.InfoFormat("Adding cache broadcast url: {0}", url);

            BroadcastNodes.Add(url);
            return true;
        }

        public static string ConvertToUrl(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName))
            {
                return string.Empty;
            }

            var url = nodeName;
            if (!url.StartsWithIgnoreCase("http"))
                url = "http://" + url;
            return url;
        }

        private bool CacheAdd(string key, object value)
        {
            return CacheAdd(key, value, DateTime.MaxValue);
        }

        private bool TryGetValue(string key, out CacheEntry entry)
        {
            return _memory.TryGetValue(key, out entry);
        }

        private void Set(string key, CacheEntry entry)
        {
            _memory[key] = entry;
        }

        /// <summary>
        /// Stores The value with key only if such key doesn't exist at the server yet. 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="expiresAt">The expires at.</param>
        /// <returns></returns>
        private bool CacheAdd(string key, object value, DateTime expiresAt)
        {
            CacheEntry entry;
            if (TryGetValue(key, out entry)) return false;

            entry = new CacheEntry(value, expiresAt);
            Set(key, entry);

            return true;
        }

        private bool CacheSet(string key, object value)
        {
            return CacheSet(key, value, DateTime.MaxValue);
        }

        private bool CacheSet(string key, object value, DateTime expiresAt)
        {
            return CacheSet(key, value, expiresAt, null);
        }

        /// <summary>
        /// Adds or replaces the value with key. 
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="expiresAt">The expires at.</param>
        /// <param name="checkLastModified">The check last modified.</param>
        /// <returns>True; if it succeeded</returns>
        private bool CacheSet(string key, object value, DateTime expiresAt, long? checkLastModified)
        {
            CacheEntry entry;
            if (!TryGetValue(key, out entry))
            {
                entry = new CacheEntry(value, expiresAt);
                Set(key, entry);
                return true;
            }

            if (checkLastModified.HasValue
                && entry.LastModifiedTicks != checkLastModified.Value) return false;

            entry.Value = value;
            entry.ExpiresAt = expiresAt;

            return true;
        }

        private bool CacheReplace(string key, object value)
        {
            return CacheReplace(key, value, DateTime.MaxValue);
        }

        private bool CacheReplace(string key, object value, DateTime expiresAt)
        {
            return !CacheSet(key, value, expiresAt);
        }

        public void Dispose()
        {
            if (!FlushOnDispose) return;

            _memory = new ConcurrentDictionary<string, CacheEntry>();
            _counters = new ConcurrentDictionary<string, int>();
        }

        public bool Remove(string key)
        {
            return Remove(key, true);
        }
        public bool Remove(string key, bool broadcast)
        {
            CacheEntry item;
            var result = _memory.TryRemove(key, out item);
            if (broadcast)
            {
                Broadcast("remove", new { key });
            }
            return result;
        }

        public void RemoveAll(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                try
                {
                    Remove(key);
                }
                catch (Exception ex)
                {
                    Log.Error(string.Format("Error trying to remove {0} from the cache", key), ex);
                }
            }
        }

        public object Get(string key)
        {
            long lastModifiedTicks;
            return Get(key, out lastModifiedTicks);
        }

        public object Get(string key, out long lastModifiedTicks)
        {
            lastModifiedTicks = 0;

            CacheEntry cacheEntry;
            if (_memory.TryGetValue(key, out cacheEntry))
            {
                if (cacheEntry.ExpiresAt < DateTime.Now)
                {
                    _memory.TryRemove(key, out cacheEntry);
                    return null;
                }
                lastModifiedTicks = cacheEntry.LastModifiedTicks;
                return cacheEntry.Value;
            }
            return null;
        }

        public T Get<T>(string key)
        {
            var value = Get(key);
            if (value != null) return (T)value;
            return default(T);
        }

        private int UpdateCounter(string key, int value)
        {
            if (!_counters.ContainsKey(key))
            {
                _counters[key] = 0;
            }
            _counters[key] += value;
            return _counters[key];
        }

        public long Increment(string key, uint amount)
        {
            return UpdateCounter(key, 1);
        }

        public long Decrement(string key, uint amount)
        {
            return UpdateCounter(key, -1);
        }

        public bool Add<T>(string key, T value)
        {
            return CacheAdd(key, value);
        }

        public bool Set<T>(string key, T value)
        {
            return CacheSet(key, value);
        }

        public bool Replace<T>(string key, T value)
        {
            return CacheReplace(key, value);
        }

        public bool Add<T>(string key, T value, DateTime expiresAt)
        {
            return CacheAdd(key, value, expiresAt);
        }

        public bool Set<T>(string key, T value, DateTime expiresAt)
        {
            return CacheSet(key, value, expiresAt);
        }

        public bool Replace<T>(string key, T value, DateTime expiresAt)
        {
            return CacheReplace(key, value, expiresAt);
        }

        public bool Add<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheAdd(key, value, DateTime.Now.Add(expiresIn));
        }

        public bool Set<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheSet(key, value, DateTime.Now.Add(expiresIn));
        }

        public bool Replace<T>(string key, T value, TimeSpan expiresIn)
        {
            return CacheReplace(key, value, DateTime.Now.Add(expiresIn));
        }

        public void FlushAll()
        {
            FlushAll(true);
        }

        public void FlushAll(bool broadcast)
        {
            _memory = new ConcurrentDictionary<string, CacheEntry>();

            if (broadcast)
            {
                Broadcast("flushall");
            }
        }

        private void Broadcast(string action, object values = null)
        {
            if (!BroadcastNodes.Any())
                return;

            var additionalValues = new StringBuilder();
            if (values != null)
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(values))
                {
                    additionalValues.Append("&");
                    additionalValues.Append(descriptor.Name);
                    additionalValues.Append("=");
                    additionalValues.Append(descriptor.GetValue(values));
                }
            }

            var httpOptions = new HttpClientHandler
                {
                    AllowAutoRedirect = true
                };
            var tasks = new List<Task>();
            foreach (var node in BroadcastNodes)
            {
                var client = new HttpClient(httpOptions);
                tasks.Add(client.GetAsync("{0}/Cache.axd?action={1}{2}".Fmt(
                    node,
                    action,
                    additionalValues.ToString()
                ), HttpCompletionOption.ResponseHeadersRead));
            }
            Task.WaitAll(tasks.ToArray());
        }

        public IDictionary<string, T> GetAll<T>(IEnumerable<string> keys)
        {
            var valueMap = new Dictionary<string, T>();
            foreach (var key in keys)
            {
                var value = Get<T>(key);
                valueMap[key] = value;
            }
            return valueMap;
        }

        public void SetAll<T>(IDictionary<string, T> values)
        {
            foreach (var entry in values)
            {
                Set(entry.Key, entry.Value);
            }
        }
    }
}