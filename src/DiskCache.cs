using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JiraToDgmlDump
{
    public sealed class DiskCache : IDisposable
    {
        private bool _disposed;

        private readonly Stream _writeStream;
        private readonly bool _waitForData;
        private readonly JsonSerializer _serializer;

        private readonly JObject _jObjectForDump;
        private readonly ConcurrentDictionary<string, JToken> _jObject;

        private readonly object _mutexForDump = new();
        private readonly ConcurrentQueue<JToken> _readerWriter = new();
        private readonly AutoResetEvent _syncEvent = new(false);
        private readonly Task _serializerTask;
        private readonly CancellationTokenSource _tokenSrc;

        public DiskCache(Stream readStream, Stream writeStream, bool waitForData)
        {
            _writeStream = writeStream;
            _waitForData = waitForData;

            _serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.DateTime,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            });

            bool leaveOpen = ReferenceEquals(readStream, writeStream);
            using var reader =
                new JsonTextReader(new StreamReader(readStream, leaveOpen: leaveOpen));
            try
            {
                var jObject = JObject.Load(reader);

                _jObject = new ConcurrentDictionary<string, JToken>(
                    jObject
                        .Children()
                        .Cast<JProperty>()
                        .Select(e => new KeyValuePair<string, JToken>(e.Name, e.Value)));

                _jObjectForDump = jObject;
            }
            catch (JsonReaderException)
            {
                _jObject = new ConcurrentDictionary<string, JToken>();
                _jObjectForDump = new JObject();
            }

            _tokenSrc = new CancellationTokenSource();
            _serializerTask = Task.Run(DumpObject, _tokenSrc.Token);
        }

        public async Task<T> Wrap<T>(string key, Func<Task<T>> loader)
        {
            T result = default;
            if (_jObject.TryGetValue(key, out var value))
            {
                using var reader = value.CreateReader();
                try
                {
                    result = _serializer.Deserialize<T>(reader);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            if (result != null && !_waitForData)
                return result;

            result = await loader();
            EnqueDumpObject(key, result);
            return result;
        }

        private void EnqueDumpObject<T>(string key, T result)
        {
            var valueAsJToken = JToken.FromObject(result, _serializer);
            var valueAsJTokenClone = valueAsJToken.DeepClone();
            if (!_jObject.TryAdd(key, valueAsJToken))
                return;

            lock (_mutexForDump)
            {
                _jObjectForDump[key] = valueAsJTokenClone;
                _readerWriter.Enqueue(_jObjectForDump.DeepClone());
            }

            _syncEvent.Set();
        }

        private void DumpObject()
        {
            try
            {
                var handles = new[] { _tokenSrc.Token.WaitHandle, _syncEvent };
                while (!_tokenSrc.Token.IsCancellationRequested)
                {
                    switch (WaitHandle.WaitAny(handles))
                    {
                        case 0: //cancel
                            return;
                        case 1: // just do the work
                            break;
                    }

                    if (!_readerWriter.TryDequeue(out var toSave))
                    {
                        // chm.....
                        return;
                    }

                    _writeStream.Position = 0;
                    using var writer =
                        new JsonTextWriter(new StreamWriter(_writeStream, Encoding.UTF8, 16 * 4096, true));
                    _serializer.Serialize(writer, toSave);
                }
            }
            finally
            {
                _writeStream.Dispose();
            }
        }

        public async Task WaitForFinish()
        {
            _tokenSrc.Cancel();
            await _serializerTask;
            Debug.Assert(_serializerTask.Status == TaskStatus.RanToCompletion);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Debug.Assert(_serializerTask.Status != TaskStatus.Running);
            _tokenSrc.Dispose();
            _writeStream.Dispose();
            _syncEvent.Dispose();
            _serializerTask.Dispose();
        }
    }
}
