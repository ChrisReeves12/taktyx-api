namespace TaktyxAPI.DTO
{
    public class LocationQueryDto
    {
        /// <summary>
        /// Center latitude for distance queries
        /// </summary>
        public double? CenterLatitude { get; set; }

        /// <summary>
        /// Center longitude for distance queries
        /// </summary>
        public double? CenterLongitude { get; set; }

        /// <summary>
        /// Search radius in meters for distance queries
        /// </summary>
        public double? RadiusInMeters { get; set; }

        /// <summary>
        /// Minimum latitude for bounding box queries
        /// </summary>
        public double? MinLatitude { get; set; }

        /// <summary>
        /// Maximum latitude for bounding box queries
        /// </summary>
        public double? MaxLatitude { get; set; }

        /// <summary>
        /// Minimum longitude for bounding box queries
        /// </summary>
        public double? MinLongitude { get; set; }

        /// <summary>
        /// Maximum longitude for bounding box queries
        /// </summary>
        public double? MaxLongitude { get; set; }
    }
}
