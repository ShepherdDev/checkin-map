using System.Collections.Generic;

using DotLiquid;
using Rock.Model;

namespace com.shepherdchurch.CheckinMap
{
    /// <summary>
    /// Helper class to pass data to the Lava code.
    /// </summary>
    public class LavaItem : ILiquidizable
    {
        /// <summary>
        /// The type of serving item this is.
        /// </summary>
        public ServingItemType Type { get; set; }

        /// <summary>
        /// The minimum number of positions needed to be filled for requirements to be met.
        /// </summary>
        public int Minimum { get; set; }

        /// <summary>
        /// The maximum number of positions that may be filled.
        /// </summary>
        public int Maximum { get; set; }

        /// <summary>
        /// The number of positions still that still need to be filled. If the Type of this
        /// item is an Area then this number may not equal Minimum - Have as sub-positions may
        /// cause the calculation to differ slightly.
        /// </summary>
        public int Need { get; set; }

        /// <summary>
        /// The number of positions that have been filled.
        /// </summary>
        public int Have { get; set; }

        /// <summary>
        /// If check-in is currently active for this position.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// The group that this item represents.
        /// </summary>
        public Group Group { get; set; }

        /// <summary>
        /// Contains the list of unique Person ID numbers that are currently checked-in to this
        /// group or its descendents.
        /// </summary>
        public IEnumerable<int> DistinctPersonIds { get; set; }

        /// <summary>
        /// Convert this object into one that Liquid recognizes.
        /// </summary>
        /// <returns>A Dictionary that contains string keys pointing to object values.</returns>
        public object ToLiquid()
        {
            Dictionary<string, object> data = new Dictionary<string, object>();

            data.Add( "Type", Type );
            data.Add( "Minimum", Minimum );
            data.Add( "Maximum", Maximum );
            data.Add( "Need", Need );
            data.Add( "Have", Have );
            data.Add( "Active", Active );
            data.Add( "Group", Group );
            data.Add( "DistinctPersonIds", DistinctPersonIds );

            return data;
        }
    }

    /// <summary>
    /// The type of serving item.
    /// </summary>
    public enum ServingItemType
    {
        /// <summary>
        /// An Area has child areas or positions underneath it.
        /// </summary>
        Area,

        /// <summary>
        /// A Position item has no child-positions or areas underneath it.
        /// </summary>
        Position
    }
}
