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
        private RockContext RockContext;
        private List<Group> Groups = null;
        private List<GroupLocation> GroupLocations = null;
        private List<GroupLocationScheduleId> GroupLocationSchedules = null;
        private List<Attendance> ActiveAttendance = null;
        private Dictionary<int, string> GroupNeedValues = null;
        private Dictionary<int, string> GroupPositionXValues = null;
        private Dictionary<int, string> GroupPositionYValues = null;
        private List<int> ScheduleIdsActive = null;

        /// <summary>
        /// Initialize a new CheckinMapHelper instance to calculate all the information needed.
        /// </summary>
        /// <param name="rockContext">The Rock Database Context to operate in.</param>
        protected CheckinMapHelper( RockContext rockContext )
        {
            this.RockContext = rockContext;
        }

        /// <summary>
        /// Get all the ImageMapItem's that need to be displayed under the parent groupId.
        /// </summary>
        /// <param name="groupId">The ID of the parent group to have buttons displayed.</param>
        /// <param name="rockPage">The RockPage object to pull information into Lava from.</param>
        /// <param name="contentTemplate">The Lava content template to use when generating the button data.</param>
        /// <param name="urlMethod">The method to call to generate a URL link for each item.</param>
        /// <returns>An enumerable collection of ImageMapItems that represent the buttons to be displayed.</returns>
        static public IEnumerable<ImageMapItem> GetImapeMapItemsForParentGroupId( int groupId, RockPage rockPage, string contentTemplate, Func<Group, bool, string> urlMethod )
        {
            return new CheckinMapHelper( new RockContext() ).GetImageMapItemsForParentGroupId( groupId, null, rockPage, contentTemplate, urlMethod );
        }

        /// <summary>
        /// Get all the ImageMapItem's that need to be displayed under the parent groupId.
        /// </summary>
        /// <param name="groupId">The ID of the parent group to have buttons displayed.</param>
        /// <param name="scheduleId">The ID of the schedule to operate in.</param>
        /// <param name="rockPage">The RockPage object to pull information into Lava from.</param>
        /// <param name="contentTemplate">The Lava content template to use when generating the button data.</param>
        /// <param name="urlMethod">The method to call to generate a URL link for each item.</param>
        /// <returns>An enumerable collection of ImageMapItems that represent the buttons to be displayed.</returns>
        static public IEnumerable<ImageMapItem> GetImapeMapItemsForParentGroupId( int groupId, int? scheduleId, RockPage rockPage, string contentTemplate, Func<Group, bool, string> urlMethod )
        {
            return new CheckinMapHelper( new RockContext() ).GetImageMapItemsForParentGroupId( groupId, scheduleId, rockPage, contentTemplate, urlMethod );
        }

        /// <summary>
        /// Get all the ImageMapItem's that need to be displayed under the parent groupId.
        /// </summary>
        /// <param name="groupId">The ID of the parent group to have buttons displayed.</param>
        /// <param name="scheduleId">The ID of the schedule to operate in.</param>
        /// <param name="rockPage">The RockPage object to pull information into Lava from.</param>
        /// <param name="contentTemplate">The Lava content template to use when generating the button data.</param>
        /// <param name="urlMethod">The method to call to generate a URL link for each item.</param>
        /// <returns>An enumerable collection of ImageMapItems that represent the buttons to be displayed.</returns>
        protected IEnumerable<ImageMapItem> GetImageMapItemsForParentGroupId( int groupId, int? scheduleId, RockPage rockPage, string contentTemplate, Func<Group, bool, string> urlMethod )
        {
            //
            // Get all the possible groups we will work with in our processing.
            //
            Groups = new GroupService( RockContext ).GetAllDescendents( groupId ).ToList();
            var groupIds = Groups.Select( g => g.Id ).ToList();

            //
            // Get all the possible GroupLocations that will be processed.
            //
            GroupLocations = new GroupLocationService( RockContext )
                .Queryable( "Schedules" )
                .Where( gl => groupIds.Contains( gl.GroupId ) ).ToList();

            if ( scheduleId.HasValue )
            {
                var schedule = new ScheduleService( RockContext ).Get( scheduleId.Value );

                if ( schedule != null && schedule.IsCheckInActive )
                {
                    ScheduleIdsActive = new List<int> { scheduleId.Value };
                }
                else
                {
                    ScheduleIdsActive = new List<int>();
                }
            }
            else
            {
                //
                // Load all the schedules we will work with. These are all schedules that are linked to any
                // group location used by any group we work with. Often this will result in a single schedule.
                //
                var schedules = new ScheduleService( RockContext )
                    .ExecuteQuery( string.Format(
                        @"
                        SELECT DISTINCT [Schedule].*
                        FROM [Schedule]
                        LEFT JOIN [GroupLocationSchedule] ON [GroupLocationSchedule].[ScheduleId] = [Schedule].[Id]
                        WHERE [GroupLocationSchedule].GroupLocationId IN ({0})",
                                string.Join( ",", GroupLocations.Select( gl => gl.Id ) ) ) )
                        .ToList();

                //
                // The IsCheckInActive method does some decent processing. Since we are likely to only have
                // a couple schedules over many groups, we pre-process the IsCheckInActive command for
                // each schedule and then we can use the list to see if the schedule Id is active later.
                //
                ScheduleIdsActive = schedules
                    .Where( s => s.IsCheckInActive )
                    .Select( s => s.Id )
                    .ToList();
            }

            //
            // Populate the list of people that are currently in attendance.
            //
            var today = RockDateTime.Now.Date;
            ActiveAttendance = new AttendanceService( this.RockContext )
                .Queryable( "PersonAlias" )
                .Where( a =>
                    a.ScheduleId.HasValue &&
                    a.GroupId.HasValue &&
                    a.LocationId.HasValue &&
                    a.PersonAlias != null &&
                    a.DidAttend.HasValue &&
                    a.DidAttend.Value &&
                    a.StartDateTime > today &&
                    !a.EndDateTime.HasValue &&
                    ScheduleIdsActive.Contains( a.ScheduleId.Value ) &&
                    groupIds.Contains( a.GroupId.Value ) )
                .ToList();

            //
            // Build the list of all GroupLocationSchedule records related to our groups.
            //
            GroupLocationSchedules = RockContext
                .Database
                .SqlQuery<GroupLocationScheduleId>(
                    string.Format( "SELECT * FROM [GroupLocationSchedule] WHERE [GroupLocationId] IN ({0})",
                        string.Join( ",", GroupLocations.Select( gl => gl.Id ) ) ) )
                .ToList();

            //
            // Pre-load all the attribute values we need in bulk.
            //
            GroupNeedValues = GetAttributeValuesForGroups( Groups, "Need", RockContext );
            GroupPositionXValues = GetAttributeValuesForGroups( Groups, "PositionX", RockContext );
            GroupPositionYValues = GetAttributeValuesForGroups( Groups, "PositionY", RockContext );

            //
            // Process all the child groups and build their ImageMapItem values.
            //
            List<ImageMapItem> items = new List<ImageMapItem>();
            foreach ( var group in Groups.Where( g => g.ParentGroupId == groupId ) )
            {
                var item = GetImageMapItemForGroup( rockPage, group, contentTemplate, urlMethod );
                if ( item != null )
                {
                    items.Add( item );
                }
            }

            return items;
        }

        /// <summary>
        /// Determine if Checkin is active for a group.
        /// </summary>
        /// <param name="groupId">The group Id to test.</param>
        /// <returns>true if any group location for the group has a schedule where check-in is currently active.</returns>
        protected bool IsCheckinActiveForGroupId( int groupId )
        {
            var glIds = GroupLocations.Where( gl => gl.GroupId == groupId ).Select( gl => gl.Id ).ToList();
            var scheduleIds = GroupLocationSchedules.Where( gls => glIds.Contains( gls.GroupLocationId ) ).Select( gls => gls.ScheduleId ).ToList();

            return ScheduleIdsActive.Where( s => scheduleIds.Contains( s ) ).Any();
        }

        /// <summary>
        /// Get the attendance count for the specified group. This counts only current attendance counts.
        /// </summary>
        /// <param name="group">The group whose attendance counts we are interested in.</param>
        /// <returns>The number of people currently checked-in to the group.</returns>
        protected int GetAttendanceCountForGroupId( int groupId )
        {
            return GetAttendanceForGroupId( groupId ).Count();
        }

        /// <summary>
        /// Get the attendance person Ids for the specified group. This returns only the unique PersonId
        /// numbers.
        /// </summary>
        /// <param name="group">The group whose attendance counts we are interested in.</param>
        /// <returns>The number of people currently checked-in to the group.</returns>
        protected IEnumerable<int> GetAttendanceForGroupId( int groupId, List<int> personIds = null )
        {
            var locationIds = GroupLocations
                .Where( gl => gl.GroupId == groupId )
                .Select( gl => gl.LocationId );
            var activeScheduleIds = GroupLocations
                .Where( gl => gl.GroupId == groupId )
                .SelectMany( gl => gl.Schedules )
                .Where( s => s.IsScheduleOrCheckInActive )
                .Select( s => s.Id )
                .ToList();

            if ( personIds == null )
            {
                personIds = new List<int>();
            }

            var currentAttendeeIds = ActiveAttendance.Where( a =>
                    activeScheduleIds.Contains( a.ScheduleId.Value ) &&
                    a.GroupId.Value == groupId )
                .Select( a => a.PersonAlias.PersonId );
            personIds.AddRange( currentAttendeeIds );

            foreach ( var grp in Groups.Where( g => g.ParentGroupId == groupId ) )
            {
                GetAttendanceForGroupId( grp.Id, personIds );
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
        protected int GetNeedForGroup( Group group, out int minimumNeed, out int maximumNeed )
        {
            int count = 0;

            minimumNeed = 0;
            maximumNeed = 0;

            var childGroups = Groups.Where( g => g.ParentGroupId == group.Id );

            if ( childGroups.Any() )
            {
                //
                // We are an area group, check all child groups.
                //
                foreach ( Group grp in childGroups )
                {
                    int minNeed, maxNeed;
                    count += GetNeedForGroup( grp, out minNeed, out maxNeed );
                    minimumNeed += minNeed;
                    maximumNeed += maxNeed;
                }
            }
            else if ( IsCheckinActiveForGroupId( group.Id ) )
            {
                //
                // This is a "need" group and check-in is active, load the attributes if needed and then calculate the need.
                //
                var val = GroupNeedValues[group.Id];
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

                need -= GetAttendanceCountForGroupId( group.Id );

                if ( need > 0 )
                {
                    count += need;
                }
            }

            return count;
        }

        /// <summary>
        /// Helper method to get the AttributeId of a given attribute key for the group.
        /// </summary>
        /// <param name="rockContext">The database context to operate in.</param>
        /// <param name="group">The Group object to load the attribute Id for.</param>
        /// <param name="attributeKey">The attribute key to process for.</param>
        /// <returns>An integer identifying the AttributeId in this group for the given key. 0 if not found.</returns>
        private static int GetAttributeIdForGroup( RockContext rockContext, Group group, string attributeKey )
        {
            Rock.Web.Cache.RockMemoryCache cache = Rock.Web.Cache.RockMemoryCache.Default;
            string cacheKey = string.Format( "com.shepherdchurch.checkinmap.attribute_{0}_{1}", attributeKey, group.Id );
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

        /// <summary>
        /// Helper method to load a single attribute value for the given group.
        /// </summary>
        /// <param name="rockContext">The database context to operate in.</param>
        /// <param name="group">The Group object to load the attribute Id for.</param>
        /// <param name="attributeKey">The attribute key to process for.</param>
        /// <returns>The raw textual representation of the value.</returns>
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
        /// Load the requested Attribute Value for a collection of groups.
        /// </summary>
        /// <param name="rockContext">The database context to operate in.</param>
        /// <param name="group">The Group object to load the attribute Id for.</param>
        /// <param name="attributeKey">The attribute key to process for.</param>
        /// <returns>A dictionary of raw attribute values. The key is the group ID number and the value is the raw textual attribute value.</returns>
        private static Dictionary<int, string> GetAttributeValuesForGroups( IEnumerable<Group> groups, string attributeKey, RockContext rockContext )
        {
            Dictionary<int, string> attributeValues = new Dictionary<int, string>();

            //
            // Process each collection of groups by their GroupTypeId together.
            //
            foreach ( var grouping in groups.GroupBy( g => g.GroupTypeId ) )
            {
                int attributeId = GetAttributeIdForGroup( rockContext, grouping.First(), attributeKey );
                var groupIds = grouping.Select( g => g.Id );

                //
                // Load the values in bulk from the database.
                //
                var values = new AttributeValueService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where( av => av.AttributeId == attributeId && av.EntityId.HasValue && groupIds.Contains( ( int ) av.EntityId ) );

                //
                // Store each value in the dictionary.
                //
                foreach ( var val in values )
                {
                    attributeValues.Add( ( int ) val.EntityId, val.Value );
                }

                //
                // Process any missing values.
                //
                foreach ( var id in groupIds )
                {
                    Rock.Web.Cache.AttributeCache attribute = null;

                    if ( !attributeValues.ContainsKey( id ) )
                    {
                        if ( attribute == null )
                        {
                            attribute = Rock.Web.Cache.AttributeCache.Read( attributeId );
                        }

                        attributeValues.Add( id, attribute != null ? attribute.DefaultValue : string.Empty );
                    }
                }
            }

            return attributeValues;
        }

        /// <summary>
        /// Gets an ImageMapItem from the given Group and lava content template.
        /// </summary>
        /// <param name="group">The group to be parsed into an ImageMapItem.</param>
        /// <param name="contentTemplate">The Lava content to use.</param>
        /// <param name="urlMethod">The method to call to generate the URL for each child group. The boolean parameter passed is true if the child group is a serving position and false if it is another sub-area.</param>
        /// <returns>A new ImageMapItem instance that represents how the Group should be displayed on the Image Map.</returns>
        protected ImageMapItem GetImageMapItemForGroup( RockPage rockPage, Group group, string contentTemplate, Func<Group, bool, string> urlMethod )
        {
            Template template;
            LavaItem servingItem = new LavaItem();
            ImageMapItem item = new ImageMapItem();
            int minNeed;
            int maxNeed;

            //
            // Setup the information in the serving item to pass to the Lava filter, also used by later
            // checks in this method.
            //
            servingItem.Group = group;
            servingItem.Need = GetNeedForGroup( group, out minNeed, out maxNeed );
            servingItem.Minimum = minNeed;
            servingItem.Maximum = maxNeed;
            var people = GetAttendanceForGroupId( group.Id );
            servingItem.Have = people.Count();
            servingItem.DistinctPersonIds = people.Distinct();
            servingItem.Active = ( servingItem.Minimum > 0 );

            if ( Groups.Where( g => g.ParentGroupId == group.Id ).Any() )
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
                if ( IsCheckinActiveForGroupId( group.Id ) )
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
            item.PositionX = GroupPositionXValues[group.Id];
            item.PositionY = GroupPositionYValues[group.Id];
            item.Identifier = group.Guid.ToString();

            return item;
        }

        class GroupLocationScheduleId
        {
            public int GroupLocationId { get; set; }
            public int ScheduleId { get; set; }
        }
    }
}
