using System;
using System.Collections.Generic;

using Rock.Plugin;

namespace com.shepherdchurch.CheckinMap.Migrations
{
    [MigrationNumber( 2, "1.5.0" )]
    public class InstallBlockTypes : CheckinMapMigration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.UpdateBlockType( "Checkin Map Details",
                "Shows and edits details of a check -in type to be used as a visual map.",
                "~/Plugins/com_shepherdchurch/CheckinMap/CheckinMapDetails.ascx",
                "com_shepherdchurch > Checkin Map",
                SystemGuid.BlockType.CHECKIN_MAP_DETAILS );

            RockMigrationHelper.UpdateBlockType( "Checkin Map Kiosk",
                "Displays Checkin Map groups in a visual format that the user can interact with to find a serving opportunity.",
                "~/Plugins/com_shepherdchurch/CheckinMap/CheckinMapKiosk.ascx",
                "com_shepherdchurch > Checkin Map",
                SystemGuid.BlockType.CHECKIN_MAP_KIOSK );

            RockMigrationHelper.UpdateBlockType( "Group Attendance Record From Url",
                "Takes a personId and groupId from the URL and marks the person as attended in that group.",
                "~/Plugins/com_shepherdchurch/CheckinMap/GroupAttendanceRecordFromUrl.ascx",
                "com_shepherdchurch > Checkin Map",
                SystemGuid.BlockType.GROUP_ATTENDANCE_RECORD_FROM_URL );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.GROUP_ATTENDANCE_RECORD_FROM_URL );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.CHECKIN_MAP_KIOSK );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.CHECKIN_MAP_DETAILS );
        }
    }
}
