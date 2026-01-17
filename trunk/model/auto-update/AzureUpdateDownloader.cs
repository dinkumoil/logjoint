using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.AutoUpdate
{
    public class AzureUpdateDownloader : IUpdateDownloader
    {
        readonly string? autoUpdateUrl;
        readonly LJTraceSource trace;

        [MemberNotNullWhen(true, nameof(autoUpdateUrl))]
        private bool IsConfigured => !string.IsNullOrEmpty(autoUpdateUrl);

        public AzureUpdateDownloader(ITraceSourceFactory traceSourceFactory, string? autoUpdateUrl, string updateType)
        {
            this.trace = traceSourceFactory.CreateTraceSource("AutoUpdater", $"az-dwnld-{updateType}");
            this.autoUpdateUrl = autoUpdateUrl;
        }

        bool IUpdateDownloader.IsDownloaderConfigured
        {
            get { return IsConfigured; }
        }

        async Task<DownloadUpdateResult> IUpdateDownloader.DownloadUpdate(string? etag, Stream targetStream, CancellationToken cancellation)
        {
            try
            {
                return await DownloadUpdateInternal(etag, targetStream, cancellation);
            }
            catch (WebException we)
            {
                trace.Error(we, "failed to download update");
                return new DownloadUpdateResult() { Status = DownloadUpdateResult.StatusCode.Failure, ErrorMessage = we.Message };
            }
        }

        async Task<DownloadUpdateResult> IUpdateDownloader.CheckUpdate(string? etag, CancellationToken cancellation)
        {
            try
            {
                return await DownloadUpdateInternal(etag, null, cancellation);
            }
            catch (WebException we)
            {
                trace.Error(we, "failed to check update");
                return new DownloadUpdateResult() { Status = DownloadUpdateResult.StatusCode.Failure, ErrorMessage = we.Message };
            }
        }

        async Task<DownloadUpdateResult> DownloadUpdateInternal(string? etag, Stream? targetStream, CancellationToken cancellation)
        {
            if (!IsConfigured)
                return new DownloadUpdateResult() { Status = DownloadUpdateResult.StatusCode.Failure };

            var request = HttpWebRequest.CreateHttp(autoUpdateUrl);
            request.Method = targetStream == null ? "HEAD" : "GET";
            if (etag != null)
            {
                request.Headers.Add(HttpRequestHeader.IfNoneMatch, etag);
            }
            using (var response = (HttpWebResponse)await request.GetResponseNoException().WithCancellation(cancellation))
            {
                if (response.StatusCode == HttpStatusCode.NotModified)
                    return new DownloadUpdateResult() { Status = DownloadUpdateResult.StatusCode.NotModified, ETag = etag };
                if (response.StatusCode != HttpStatusCode.OK)
                    return new DownloadUpdateResult()
                    {
                        Status = DownloadUpdateResult.StatusCode.Failure,
                        ErrorMessage = string.Format("{0} {1}", response.StatusCode, response.StatusDescription)
                    };
                if (targetStream != null)
                {
                    await response.GetResponseStream().CopyToAsync(targetStream);
                }
                string? lastModifiedUtcStr = response.Headers[HttpResponseHeader.LastModified];
                return new DownloadUpdateResult()
                {
                    Status = DownloadUpdateResult.StatusCode.Success,
                    ETag = response.Headers[HttpResponseHeader.ETag],
                    LastModifiedUtc = lastModifiedUtcStr != null ? DateTime.Parse(lastModifiedUtcStr,
                        null, DateTimeStyles.AdjustToUniversal) : null
                };
            }
        }
    }
}
