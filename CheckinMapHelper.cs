using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using DotLiquid;
using Rock;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.UI;

namespace com.shepherdchurch.CheckinMap
{
    /// <summary>
    /// Provides helper methods for working with the Serving Map suite of blocks.
    /// </summary>
    public class CheckinMapHelper
    {
        /// <summary>
        /// Get the attendance count for the specified group. This counts only current attendance counts.
        /// </summary>
        /// <param name="group">The group whose attendance counts we are interested in.</param>
        /// <returns>The number of people currently checked-in to the group.</returns>
        static public int GetAttendanceCountForGroup( Group group )
        {
            return GetAttendanceForGroup( group ).Count();
        }

        /// <summary>
        /// Get the attendance person Ids for the specified group. This returns only the unique PersonId
        /// numbers.
        /// </summary>
        /// <param name="group">The group whose attendance counts we are interested in.</param>
        /// <returns>The number of people currently checked-in to the group.</returns>
        static public IEnumerable<int> GetAttendanceForGroup( Group group, List<int> personIds = null )
        {
            if ( personIds == null )
            {
                personIds = new List<int>();
            }

            foreach ( var location in group.GroupLocations )
            {
                var summary = Rock.CheckIn.KioskLocationAttendance.Read( location.LocationId );

                var people = summary
                    .Groups
                    .Where( g => g.GroupId == group.Id )
                    .SelectMany( g => g.Schedules )
                    .SelectMany( s => s.PersonIds );

                personIds.AddRange( people );
            }

            foreach ( Group grp in group.Groups )
            {
                GetAttendanceForGroup( grp, personIds );
            }

            return personIds;
        }

        /// <summary>
        /// Calculate the need for a group. Calculates need for child groups too. This returns the
        /// combined minimum need of this group and all child groups.
        /// </summary>
        /// <param name="group">The parent group to start calculating need from.</param>
        /// <param name="remainingNeed">If true only the remaining minimum need is returned.</param>
        /// <returns>An integer that identifies the number of spots that need to be filled.</returns>
        static public int GetNeedForGroup( Group group, out int minimumNeed, out int maximumNeed, Rock.Data.RockContext rockContext )
        {
            int count = 0;

            minimumNeed = 0;
            maximumNeed = 0;

            if ( group.Groups.Count > 0 )
            {
                //
                // We are an area group, check all child groups.
                //
                foreach ( Group grp in group.Groups )
                {
                    int minNeed, maxNeed;
                    count += GetNeedForGroup( grp, out minNeed, out maxNeed, rockContext );
                    minimumNeed += minNeed;
                    maximumNeed += maxNeed;
                }
            }
            else if ( group.GroupLocations.SelectMany( gl => gl.Schedules ).Where( s => s.IsCheckInActive ).Any() ) // FIXME: This is adding 0.4 seconds to page load
            {
                //
                // This is a "need" group and check-in is active, load the attributes if needed and then calculate the need.
                //
                var val = GetAttributeValue( rockContext, group, "Need" );
                int need = 0;

                if ( !string.IsNullOrWhiteSpace( val ) )
                {
                    var vals = val.Split( ',' );

                    if ( vals.Length == 2 )
                    {
                        need = vals[0].AsInteger();
                        minimumNeed += need;
                        maximumNeed += vals[1].AsInteger();
                    }
                }

                need -= GetAttendanceCountForGroup( group );

                if ( need > 0 )
                {
                    count += need;
                }
            }

            return count;
        }

        private static int GetAttributeIdForGroup( RockContext rockContext, Group group, string attributeKey )
        {
            Rock.Web.Cache.RockMemoryCache cache = Rock.Web.Cache.RockMemoryCache.Default;
            string cacheKey = string.Format( "com.shepherdchurch.checkinmap.attribute_{0}_{1}", attributeKey, group.GroupTypeId );
            var val = cache.Get( cacheKey );

            if ( val == null )
            {
                if ( group.Attributes == null )
                {
                    group.LoadAttributes( rockContext );
                }

                var attribute = group
                    .Attributes
                    .Where( a => a.Key == attributeKey )
                    .Select( a => a.Value )
                    .FirstOrDefault();

                if ( attribute != null )
                {
                    val = attribute.Id;
                    cache.Add( cacheKey, val, DateTime.Now.AddMinutes( 10 ) );

                    return attribute.Id;
                }

                return 0;
            }

            return ( int ) val;
        }

        private static string GetAttributeValue( RockContext rockContext, Group group, string attributeKey )
        {
            int attributeId = GetAttributeIdForGroup( rockContext, group, attributeKey );

            var val = new AttributeValueService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( av => av.AttributeId == attributeId && av.EntityId == group.Id )
                .Select( av => av.Value )
                .FirstOrDefault();

            if ( val == null )
            {
                var attribute = Rock.Web.Cache.AttributeCache.Read( attributeId );

                return attribute != null ? attribute.DefaultValue : string.Empty;
            }

            return val;
        }

        /// <summary>
        /// Gets an ImageMapItem from the given Group and lava content template.
        /// </summary>
        /// <param name="group">The group to be parsed into an ImageMapItem.</param>
        /// <param name="contentTemplate">The Lava content to use.</param>
        /// <param name="urlMethod">The method to call to generate the URL for each child group. The boolean parameter passed is true if the child group is a serving position and false if it is another sub-area.</param>
        /// <returns>A new ImageMapItem instance that represents how the Group should be displayed on the Image Map.</returns>
        static public ImageMapItem GetImageMapItemForGroup( RockPage rockPage, Group group, string contentTemplate, Func<Group, bool, string> urlMethod, Rock.Data.RockContext rockContext = null )
        {
            Template template;
            LavaItem servingItem = new LavaItem();
            ImageMapItem item = new ImageMapItem();
            int minNeed;
            int maxNeed;

            if ( rockContext == null )
            {
                rockContext = new RockContext();
            }

            if ( group.Attributes == null )
            {
                group.LoadAttributes( rockContext );
            }

            //
            // Setup the information in the serving item to pass to the Lava filter, also used by later
            // checks in this method.
            //
            servingItem.Group = group;
            servingItem.Need = GetNeedForGroup( group, out minNeed, out maxNeed, rockContext );
            servingItem.Minimum = minNeed;
            servingItem.Maximum = maxNeed;
            var people = GetAttendanceForGroup( group );
            servingItem.Have = people.Count();
            servingItem.DistinctPersonIds = people.Distinct();
            servingItem.Active = ( servingItem.Minimum > 0 );

            if ( group.Groups.Count > 0 )
            {
                //
                // This block itself does not do check-in, but it has child-groups to display.
                // This will cause a postback so we can redraw with the selected group.
                //
                item.Url = ( urlMethod != null ? urlMethod( group, false ) : null );
                servingItem.Type = ServingItemType.Area;
            }
            else
            {
                //
                // This is a group block for check-in. Make sure check-in is active for it.
                //
                if ( group.GroupLocations.SelectMany( gl => gl.Schedules ).Where( s => s.IsCheckInActive ).Any() )
                {
                    if ( servingItem.Have < servingItem.Maximum )
                    {
                        item.Url = ( urlMethod != null ? urlMethod( group, true ) : null );
                    }
                }
                else
                {
                    servingItem.Active = false;
                }

                servingItem.Type = ServingItemType.Position;
            }

            //
            // Setup the lava template to run with the variables we pass in.
            //
            template = Template.Parse( contentTemplate );
            var commonMergeFields = LavaHelper.GetCommonMergeFields( rockPage, null, new CommonMergeFieldsOptions { GetLegacyGlobalMergeFields = false } );
            foreach ( var field in commonMergeFields )
            {
                template.InstanceAssigns.Add( field.Key, field.Value );
            }
            template.InstanceAssigns.Add( "Title", group.Name );
            template.InstanceAssigns.Add( "CssClass", string.Empty );
            template.InstanceAssigns.Add( "Item", servingItem );
            template.InstanceAssigns.Add( "Url", item.Url );

            //
            // Run the lava and take the rendered output as the text for the block.
            //
            item.Text = template.Render();

            //
            // Extract from the lava run the Title and CssClass variables and set positions.
            //
            item.Title = template.InstanceAssigns["Title"].ToString();
            item.CssClass = template.InstanceAssigns["CssClass"].ToString();
            item.Url = template.InstanceAssigns["Url"] != null ? template.InstanceAssigns["Url"].ToString() : null;
            item.PositionX = group.GetAttributeValue( "PositionX" );
            item.PositionY = group.GetAttributeValue( "PositionY" );
            item.Identifier = group.Guid.ToString();

            return item;
        }

        public static long RunTest( RockContext rockContext, RockPage rockPage, int groupId )
        {
            string template = @"Spots: {{ Item.Minimum }}{% if Item.Minimum != Item.Maximum %} - {{ Item.Maximum }}{% endif %}<br />Have: {{ Item.Have }}<br />Need: {{ Item.Need }}
{%- if Item.Active == 'false' -%}
 {%- assign CssClass = 'disabled' -%}
{%- elseif Item.Need > 0 -%}
 {%- assign CssClass = 'imagemap-danger' -%}
{%- elseif Item.Have < Item.Maximum -%}
 {%- assign CssClass = 'imagemap-warning' -%}
{%- else -%}
 {%- assign CssClass = 'imagemap-success' -%}
{%- endif -%}";
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            sw.Start();
            var groups = new GroupService( rockContext )
                .Queryable( "GroupLocations,GroupLocations.Schedules,Groups,Groups.Groups,Groups.GroupLocations,Groups.GroupLocations.Schedules" )
                .AsNoTracking()
                .Where( g => g.ParentGroupId == groupId );
            foreach ( var grp in groups )
            {
                CheckinMapHelper.GetImageMapItemForGroup( rockPage, grp, template, null, rockContext );
            }
            sw.Stop();

            return sw.ElapsedMilliseconds;
        }
    }
}
