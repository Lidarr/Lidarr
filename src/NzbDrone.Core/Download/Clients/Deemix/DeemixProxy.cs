using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using SocketIOClient;

namespace NzbDrone.Core.Download.Clients.Deemix
{
    public class DeemixClientItem : DownloadClientItem
    {
        public DateTime? Started { get; set; }
    }

    public class DeemixProxy : IDisposable
    {
        private static readonly Dictionary<string, long> Bitrates = new Dictionary<string, long>
        {
            { "1", 128 },
            { "3", 320 },
            { "9", 1000 }
        };
        private static readonly Dictionary<string, string> Formats = new Dictionary<string, string>
        {
            { "1", "MP3 128" },
            { "3", "MP3 320" },
            { "9", "FLAC" }
        };
        private static int AddId;

        private readonly Logger _logger;
        private readonly ManualResetEventSlim _connected;
        private readonly Dictionary<int, DeemixPendingItem<string>> _pendingAdds;
        private readonly Dictionary<int, DeemixPendingItem<DeemixSearchResponse>> _pendingSearches;

        private bool _disposed;
        private SocketIO _client;
        private List<DeemixClientItem> _queue;
        private DeemixConfig _config;

        public DeemixProxy(string url,
                           Logger logger)
        {
            _logger = logger;

            _connected = new ManualResetEventSlim(false);
            _queue = new List<DeemixClientItem>();

            _pendingAdds = new Dictionary<int, DeemixPendingItem<string>>();
            _pendingSearches = new Dictionary<int, DeemixPendingItem<DeemixSearchResponse>>();

            Connect(url);
        }

        public DeemixConfig GetSettings()
        {
            if (!_connected.Wait(5000))
            {
                throw new DownloadClientUnavailableException("Deemix not connected");
            }

            return _config;
        }

        public List<DeemixClientItem> GetQueue()
        {
            if (!_connected.Wait(5000))
            {
                throw new DownloadClientUnavailableException("Deemix not connected");
            }

            return _queue;
        }

        public void RemoveFromQueue(string downloadId)
        {
            _client.EmitAsync("removeFromQueue", downloadId);
        }

        public string Download(string url, int bitrate)
        {
            if (!_connected.Wait(5000))
            {
                throw new DownloadClientUnavailableException("Deemix not connected");
            }

            _logger.Trace($"Downloading {url} bitrate {bitrate}");

            using (var pending = new DeemixPendingItem<string>())
            {
                var ack = Interlocked.Increment(ref AddId);
                _pendingAdds[ack] = pending;

                _client.EmitAsync("addToQueue",
                                  new
                                  {
                                      url,
                                      bitrate,
                                      ack
                                  });

                _logger.Trace($"Awaiting result for add {ack}");
                var added = pending.Wait(60000);

                _pendingAdds.Remove(ack);

                if (!added)
                {
                    throw new DownloadClientUnavailableException("Could not add item");
                }

                return pending.Item;
            }
        }

        public DeemixSearchResponse Search(string term, int count, int offset)
        {
            if (!_connected.Wait(5000))
            {
                throw new DownloadClientUnavailableException("Deemix not connected");
            }

            _logger.Trace($"Searching for {term}");

            using (var pending = new DeemixPendingItem<DeemixSearchResponse>())
            {
                var ack = Interlocked.Increment(ref AddId);
                _pendingSearches[ack] = pending;

                _client.EmitAsync("albumSearch",
                                  new
                                  {
                                      term,
                                      start = offset,
                                      nb = count,
                                      ack
                                  });

                _logger.Trace($"Awaiting result for search {ack}");
                var gotResult = pending.Wait(60000);

                _pendingSearches.Remove(ack);

                if (!gotResult)
                {
                    throw new DownloadClientUnavailableException("Could not search for {0}", term);
                }

                return pending.Item;
            }
        }

        public DeemixSearchResponse RecentReleases()
        {
            if (!_connected.Wait(5000))
            {
                throw new DownloadClientUnavailableException("Deemix not connected");
            }

            using (var pending = new DeemixPendingItem<DeemixSearchResponse>())
            {
                var ack = Interlocked.Increment(ref AddId);
                _pendingSearches[ack] = pending;

                _client.EmitAsync("newReleases",
                                  new
                                  {
                                      ack
                                  });

                _logger.Trace($"Awaiting result for RSS {ack}");
                var gotResult = pending.Wait(60000);

                _pendingSearches.Remove(ack);

                if (!gotResult)
                {
                    throw new DownloadClientUnavailableException("Could not get recent releases");
                }

                return pending.Item;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed state (managed objects).
                Disconnect();
                _connected.Dispose();
            }

            _disposed = true;
        }

        private void Disconnect()
        {
            if (_client != null)
            {
                _logger.Trace("Disconnecting");
                _client.DisconnectAsync().GetAwaiter().GetResult();
            }
        }

        private void Connect(string url)
        {
            _queue = new List<DeemixClientItem>();

            var options = new SocketIOOptions
            {
                ReconnectionDelay = 1000,
                ReconnectionDelayMax = 10000
            };

            _client = new SocketIO(url, options);
            _client.OnConnected += (s, e) =>
            {
                _logger.Trace("connected");
                _connected.Set();
            };

            _client.OnDisconnected += (s, e) =>
            {
                _logger.Trace("disconnected");
                _connected.Reset();
            };

            _client.OnPing += (s, e) => _logger.Trace("ping");
            _client.OnPong += (s, e) => _logger.Trace("pong");

            _client.OnReconnecting += (s, e) => _logger.Trace("reconnecting");
            _client.OnError += (s, e) => _logger.Warn($"error {e}");

            _client.On("init_settings", ErrorHandler(OnSettings, true));
            _client.On("updateSettings", ErrorHandler(OnSettings, true));
            _client.On("init_downloadQueue", ErrorHandler(OnInitQueue, true));
            _client.On("addedToQueue", ErrorHandler(OnAddedToQueue, true));
            _client.On("updateQueue", ErrorHandler(OnUpdateQueue, true));
            _client.On("finishDownload", ErrorHandler(response => UpdateDownloadStatus(response, DownloadItemStatus.Completed), true));
            _client.On("startDownload", ErrorHandler(response => UpdateDownloadStatus(response, DownloadItemStatus.Downloading), true));
            _client.On("removedFromQueue", ErrorHandler(OnRemovedFromQueue, true));
            _client.On("removedAllDownloads", ErrorHandler(OnRemovedAllFromQueue, true));
            _client.On("removedFinishedDownloads", ErrorHandler(OnRemovedFinishedFromQueue, true));
            _client.On("loginNeededToDownload", OnLoginRequired);
            _client.On("queueError", OnQueueError);
            _client.On("albumSearch", ErrorHandler(OnSearchResult, false));
            _client.On("newReleases", ErrorHandler(OnSearchResult, false));

            _logger.Trace("Connecting to deemix");
            _connected.Reset();
            _client.ConnectAsync();

            _logger.Trace("waiting for connection");
            if (!_connected.Wait(60000))
            {
                throw new DownloadClientUnavailableException("Unable to connect to Deemix");
            }
        }

        private Action<SocketIOResponse> ErrorHandler(Action<SocketIOResponse> action, bool logResponse)
        {
            return (SocketIOResponse x) =>
            {
                if (logResponse)
                {
                    _logger.Trace(x.ToString());
                }

                try
                {
                    action(x);
                }
                catch (Exception e)
                {
                    e.Data.Add("response", x.GetValue().ToString());
                    _logger.Error(e, "Deemix error");
                }
            };
        }

        private void OnSettings(SocketIOResponse response)
        {
            _config = response.GetValue<DeemixConfig>();
        }

        private void OnInitQueue(SocketIOResponse response)
        {
            var dq = response.GetValue<DeemixQueue>();

            var items = dq.QueueList.Values.ToList();

            _queue = items.Select(x => ToDownloadClientItem(x)).ToList();
        }

        private void OnUpdateQueue(SocketIOResponse response)
        {
            var item = response.GetValue<DeemixQueueUpdate>();

            var queueItem = _queue.SingleOrDefault(x => x.DownloadId == item.Uuid);
            if (queueItem == null)
            {
                return;
            }

            if (item.Progress.HasValue)
            {
                var progress = Math.Min(item.Progress.Value, 100) / 100.0;
                queueItem.RemainingSize = (long)((1 - progress) * queueItem.TotalSize);

                if (queueItem.Started.HasValue)
                {
                    var elapsed = DateTime.UtcNow - queueItem.Started;
                    queueItem.RemainingTime = TimeSpan.FromTicks((long)(elapsed.Value.Ticks * (1 - progress) / progress));
                }
            }

            if (item.ExtrasPath.IsNotNullOrWhiteSpace())
            {
                queueItem.OutputPath = new OsPath(item.ExtrasPath);
            }
        }

        private void UpdateDownloadStatus(SocketIOResponse response, DownloadItemStatus status)
        {
            var uuid = response.GetValue<string>();

            var queueItem = _queue.SingleOrDefault(x => x.DownloadId == uuid);
            if (queueItem != null)
            {
                queueItem.Status = status;

                if (status == DownloadItemStatus.Downloading)
                {
                    queueItem.Started = DateTime.UtcNow;
                }
            }
        }

        private void OnRemovedFromQueue(SocketIOResponse response)
        {
            var uuid = response.GetValue<string>();

            var queueItem = _queue.SingleOrDefault(x => x.DownloadId == uuid);
            if (queueItem != null)
            {
                _queue.Remove(queueItem);
            }
        }

        private void OnRemovedAllFromQueue(SocketIOResponse response)
        {
            _queue = new List<DeemixClientItem>();
        }

        private void OnRemovedFinishedFromQueue(SocketIOResponse response)
        {
            _queue = _queue.Where(x => x.Status != DownloadItemStatus.Completed).ToList();
        }

        private void OnAddedToQueue(SocketIOResponse response)
        {
            DeemixQueueItem item;

            if (response.GetValue().Type == JTokenType.Object)
            {
                item = response.GetValue<DeemixQueueItem>();
            }
            else if (response.GetValue().Type == JTokenType.Array)
            {
                var list = response.GetValue<List<DeemixQueueItem>>();

                if (list.Count != 1)
                {
                    _logger.Trace("New item not a single release, skipping");
                    return;
                }

                item = list.Single();
            }
            else
            {
                _logger.Trace("New queue item response not of recognised form, skipping");
                return;
            }

            if (item.Type != "album" && item.Type != "track")
            {
                _logger.Trace("New queue item not album or track, skipping");
                return;
            }

            if (item.Ack.HasValue && _pendingAdds.TryGetValue(item.Ack.Value, out var pending))
            {
                pending.Item = item.Uuid;
                pending.Ack();
            }

            var dci = ToDownloadClientItem(item);
            _queue.Add(dci);
        }

        private void OnQueueError(SocketIOResponse response)
        {
            var error = response.GetValue().ToString();
            _logger.Error($"Queue error:\n {error}");
        }

        private void OnSearchResult(SocketIOResponse response)
        {
            var result = response.GetValue<DeemixSearchResponse>();

            if (result.Ack.HasValue && _pendingSearches.TryGetValue(result.Ack.Value, out var pending))
            {
                pending.Item = result;
                pending.Ack();
            }
        }

        private void OnLoginRequired(SocketIOResponse response)
        {
            throw new DownloadClientUnavailableException("login required");
        }

        private static DeemixClientItem ToDownloadClientItem(DeemixQueueItem x)
        {
            var title = $"{x.Artist} - {x.Title} [WEB] {Formats[x.Bitrate]}";
            if (x.Explicit)
            {
                title += " [Explicit]";
            }

            // assume 3 mins per track, bitrates in kbps
            var size = x.Size * 180L * Bitrates[x.Bitrate] * 128L;

            var item = new DeemixClientItem
            {
                DownloadId = x.Uuid,
                Title = title,
                TotalSize = size,
                RemainingSize = (long)(1 - (x.Progress / 100.0)) * size,
                Status = GetItemStatus(x),
                CanMoveFiles = true,
                CanBeRemoved = true
            };

            if (x.ExtrasPath.IsNotNullOrWhiteSpace())
            {
                item.OutputPath = new OsPath(x.ExtrasPath);
            }

            return item;
        }

        private static DownloadItemStatus GetItemStatus(DeemixQueueItem item)
        {
            if (item.Failed)
            {
                return DownloadItemStatus.Failed;
            }

            if (item.Progress == 0)
            {
                return DownloadItemStatus.Queued;
            }

            if (item.Progress < 100)
            {
                return DownloadItemStatus.Downloading;
            }

            return DownloadItemStatus.Completed;
        }
    }
}
