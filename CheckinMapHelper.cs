﻿using System;
using System.Linq;

using DotLiquid;
using Rock;
using Rock.Model;

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
            int count = 0;

            foreach ( var location in group.GroupLocations )
            {
                var summary = Rock.CheckIn.KioskLocationAttendance.Read( location.LocationId );
                var gSummary = summary.Groups.Where( g => g.GroupId == group.Id ).FirstOrDefault();

                if ( gSummary != null )
                {
                    count += gSummary.CurrentCount;
                }
            }

            foreach ( Group grp in group.Groups )
            {
                count += GetAttendanceCountForGroup( grp );
            }

            return count;
        }

        /// <summary>
        /// Get the minimum need for the specified group and descendents.
        /// </summary>
        /// <param name="group">The group whose minimum need we are interested in.</param>
        /// <returns>The number of people needed for this group.</returns>
        static public int GetMinimumNeedForGroup( Group group )
        {
            int count = 0;

            if ( group.Attributes == null )
            {
                group.LoadAttributes();
            }
            if ( group.GetAttributeValues( "Need" ).Count == 2 )
            {
                count += group.GetAttributeValues( "Need" )[0].AsInteger();
            }

            foreach ( Group grp in group.Groups )
            {
                count += GetMinimumNeedForGroup( grp );
            }

            return count;
        }

        /// <summary>
        /// Get the maximum need for the specified group and descendents.
        /// </summary>
        /// <param name="group">The group whose maximum need we are interested in.</param>
        /// <returns>The number of people that can serve for this group.</returns>
        static public int GetMaximumNeedForGroup( Group group )
        {
            int count = 0;

            if ( group.Attributes == null )
            {
                group.LoadAttributes();
            }
            if ( group.GetAttributeValues( "Need" ).Count == 2 )
            {
                count += group.GetAttributeValues( "Need" )[1].AsInteger();
            }

            foreach ( Group grp in group.Groups )
            {
                count += GetMaximumNeedForGroup( grp );
            }

            return count;
        }

        /// <summary>
        /// Calculate the need for a group. Calculates need for child groups too. This returns the
        /// combined minimum need of this group and all child groups.
        /// </summary>
        /// <param name="group">The parent group to start calculating need from.</param>
        /// <param name="remainingNeed">If true only the remaining minimum need is returned.</param>
        /// <returns>An integer that identifies the number of spots that need to be filled.</returns>
        static public int GetNeedForGroup( Group group, bool remainingNeed = true )
        {
            int count = 0;

            if ( group.Groups.Count > 0 )
            {
                //
                // We are an area group, check all child groups.
                //
                foreach ( Group grp in group.Groups )
                {
                    count += GetNeedForGroup( grp );
                }
            }
            else
            {
                //
                // This is a "need" group, load the attributes if needed and then calculate the need.
                //
                if ( group.Attributes == null )
                {
                    group.LoadAttributes();
                }

                int need = 0;
                if ( group.GetAttributeValues( "Need" ).Count == 2 )
                {
                    need = group.GetAttributeValues( "Need" )[0].AsInteger();
                }

                if ( remainingNeed )
                {
                    need -= GetAttendanceCountForGroup( group );
                }

                if ( need > 0 )
                {
                    count += need;
                }
            }

            return count;
        }

        /// <summary>
        /// Gets an ImageMapItem from the given Group and lava content template.
        /// </summary>
        /// <param name="group">The group to be parsed into an ImageMapItem.</param>
        /// <param name="contentTemplate">The Lava content to use.</param>
        /// <param name="urlMethod">The method to call to generate the URL for each child group. The boolean parameter passed is true if the child group is a serving position and false if it is another sub-area.</param>
        /// <returns>A new ImageMapItem instance that represents how the Group should be displayed on the Image Map.</returns>
        static public ImageMapItem GetImageMapItemForGroup( Group group, string contentTemplate, Func<Group, bool, string> urlMethod )
        {
            Template template;
            LavaItem servingItem = new LavaItem();
            ImageMapItem item = new ImageMapItem();

            if ( group.Attributes == null )
            {
                group.LoadAttributes();
            }

            //
            // Setup the information in the serving item to pass to the Lava filter, also used by later
            // checks in this method.
            //
            servingItem.Group = group;
            servingItem.Need = GetNeedForGroup( group );
            servingItem.Have = GetAttendanceCountForGroup( group );
            servingItem.Minimum = GetMinimumNeedForGroup( group );
            servingItem.Maximum = GetMaximumNeedForGroup( group );
            servingItem.Active = true;

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
            template.InstanceAssigns.Add( "Title", group.Name );
            template.InstanceAssigns.Add( "CssClass", string.Empty );
            template.InstanceAssigns.Add( "Item", servingItem );

            //
            // Run the lava and take the rendered output as the text for the block.
            //
            item.Text = template.Render();

            //
            // Extract from the lava run the Title and CssClass variables and set positions.
            //
            item.Title = template.InstanceAssigns["Title"].ToString();
            item.CssClass = template.InstanceAssigns["CssClass"].ToString();
            item.PositionX = group.GetAttributeValue( "PositionX" );
            item.PositionY = group.GetAttributeValue( "PositionY" );
            item.Identifier = group.Guid.ToString();

            return item;
        }
    }
}