#pragma warning disable CS1591

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.IO
{
    public class StreamHelper : IStreamHelper
    {
        private const int StreamCopyToBufferSize = 81920;
        private readonly object _callbackLock = new object();

        public async Task CopyToAsync(Stream source, Stream destination, int bufferSize, Action onStarted, CancellationToken cancellationToken)
        {
            await CopyToAsyncInternal(source, destination, cancellationToken, bufferSize, onStarted: onStarted).ConfigureAwait(false);
        }

        public async Task CopyToAsync(Stream source, Stream destination, int bufferSize, int emptyReadLimit, CancellationToken cancellationToken)
        {
            await CopyToAsyncInternal(source, destination, cancellationToken, bufferSize, emptyReadLimit).ConfigureAwait(false);
        }

        public async Task<int> CopyToAsync(Stream source, Stream destination, CancellationToken cancellationToken)
        {
            return await CopyToAsyncInternal(source, destination, cancellationToken).ConfigureAwait(false);
        }

        public async Task CopyToAsync(Stream source, Stream destination, long copyLength, CancellationToken cancellationToken)
        {
            await CopyToAsyncInternal(source, destination, cancellationToken, copyLength: copyLength).ConfigureAwait(false);
        }

        public async Task CopyUntilCancelled(Stream source, Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            await CopyToAsyncInternal(source, destination, cancellationToken, bufferSize, copyUntilCancelled: true).ConfigureAwait(false);
        }

        private async ValueTask<int> CopyToAsyncInternal(
            Stream source,
            Stream destination,
            CancellationToken cancellationToken,
            int? buffersize = null,
            int? emptyReadLimit = null,
            long? copyLength = null,
            Action onStarted = null,
            bool copyUntilCancelled = false)
        {
            var totalBytesTransferred = 0;
            var eofCount = 0;

            Action callback = () =>
            {
                lock (_callbackLock)
                {
                    Task.Run(onStarted ?? (() => { }));
                    callback = null;
                    onStarted = null;
                }
            };

            buffersize = buffersize ?? StreamCopyToBufferSize;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(buffersize.Value);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await source.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false);
                    int bytesToWrite = copyLength.HasValue ? (int)Math.Min(bytesRead, copyLength.Value) : bytesRead;

                    if (bytesRead == 0)
                    {
                        if (copyUntilCancelled || ++eofCount < emptyReadLimit) // if emptyReadLimit is null this will evaulate to false
                        {
                            var delay = copyUntilCancelled ? 100 : 50; // matching original version but is it really different in each case ?
                            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        eofCount = 0;
                        await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesToWrite), cancellationToken).ConfigureAwait(false);
                        totalBytesTransferred += bytesToWrite;

                        callback?.Invoke();
                    }

                    if (copyLength.HasValue && (copyLength -= bytesToWrite) <= 0)
                        break;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return totalBytesTransferred;
        }
    }
}
