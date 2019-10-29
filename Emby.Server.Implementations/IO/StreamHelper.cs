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
        private object _callbackLock = new object();

        public async Task CopyToAsync(Stream source, Stream destination, int bufferSize, Action onStarted, CancellationToken cancellationToken)
        {
            await CopyToAsyncInternal(source, destination, cancellationToken, buffersize: bufferSize, onStarted: onStarted).ConfigureAwait(false);
        }

        public async Task CopyToAsync(Stream source, Stream destination, int bufferSize, int emptyReadLimit, CancellationToken cancellationToken)
        {
            await CopyToAsyncInternal(source, destination, cancellationToken, buffersize: bufferSize, emptyReadLimit: emptyReadLimit).ConfigureAwait(false);
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
            await CopyToAsyncInternal(source, destination, cancellationToken, buffersize: bufferSize, copyUntilCancelled: true).ConfigureAwait(false);
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
                        if (copyUntilCancelled)
                        {
                            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                        }
                        else if (emptyReadLimit.HasValue && ++eofCount < emptyReadLimit)
                        {
                            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
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

                    if (copyLength.HasValue)
                    {
                        if ((copyLength -= bytesToWrite) <= 0)
                            break;
                    }
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
