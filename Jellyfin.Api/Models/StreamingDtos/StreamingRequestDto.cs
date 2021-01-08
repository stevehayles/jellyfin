﻿using MediaBrowser.Controller.MediaEncoding;

namespace Jellyfin.Api.Models.StreamingDtos
{
    /// <summary>
    /// The audio streaming request dto.
    /// </summary>
    public class StreamingRequestDto : BaseEncodingJobOptions
    {
        /// <summary>
        /// Gets or sets the device profile.
        /// </summary>
        public string? DeviceProfileId { get; set; }

        /// <summary>
        /// Gets or sets the params.
        /// </summary>
        public string? Params { get; set; }

        /// <summary>
        /// Gets or sets the play session id.
        /// </summary>
        public string? PlaySessionId { get; set; }

        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        public string? Tag { get; set; }

        /// <summary>
        /// Gets or sets the segment container.
        /// </summary>
        public string? SegmentContainer { get; set; }

        /// <summary>
        /// Gets or sets the segment length.
        /// </summary>
        public int? SegmentLength { get; set; }

        /// <summary>
        /// Gets or sets the min segments.
        /// </summary>
        public int? MinSegments { get; set; }
    }
}
