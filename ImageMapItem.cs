using Newtonsoft.Json;

namespace com.shepherdchurch.CheckinMap
{
    /// <summary>
    /// Helper class for converting data into JSON syntax.
    /// </summary>
    public class ImageMapItem
    {
        /// <summary>
        /// The unique identifier of this particular item.
        /// </summary>
        [JsonProperty( "id" )]
        public string Identifier { get; set; }

        /// <summary>
        /// The additional CSS Class(es) to be applied to the imagemap-button item.
        /// </summary>
        [JsonProperty( "class" )]
        public string CssClass { get; set; }

        /// <summary>
        /// The contents of the imagemap-title element;
        /// </summary>
        [JsonProperty( "title" )]
        public string Title { get; set; }

        /// <summary>
        /// The contents of the imagemap-text element.
        /// </summary>
        [JsonProperty( "text" )]
        public string Text { get; set; }

        /// <summary>
        /// The center-x position (usually as a percentage) of the imagemap-button element within it's parent.
        /// </summary>
        [JsonProperty( "x" )]
        public string PositionX { get; set; }

        /// <summary>
        /// The center-y position (usually as a percentage) of the imagemap-button element within it's parent.
        /// </summary>
        [JsonProperty( "y" )]
        public string PositionY { get; set; }

        /// <summary>
        /// The URL to apply to the href attribute of the imagemap-button element. If blank then no hyperlink
        /// is setup.
        /// </summary>
        [JsonProperty( "url" )]
        public string Url { get; set; }
    }
}
