using System;
using System.Collections.Generic;

using Rock.Plugin;

namespace com.shepherdchurch.CheckinMap.Migrations
{
    [MigrationNumber( 3, "1.5.0" )]
    public class InstallPages : CheckinMapMigration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddPage( Rock.SystemGuid.Page.CHECK_IN_ROCK_SETTINGS,
                "D65F783D-87A9-4CC9-8110-E83466A0EADB" /* Full Width Layout */,
                "Check-in Map Configuration",
                "Configure the map settings for groups that inherit from the Check In Map group type.",
                SystemGuid.Page.CHECK_IN_MAP_CONFIGURATION,
                "fa fa-map" );

            RockMigrationHelper.AddPage( "5B6DBC42-8B03-4D15-8D92-AAFA28FD8616" /* Plugin Page */,
                "D65F783D-87A9-4CC9-8110-E83466A0EADB" /* Full Width Layout */,
                "Check-in Maps",
                "Post installation instructions.",
                SystemGuid.Page.PLUGIN_CONFIGURATION,
                "fa fa-map" );

            RockMigrationHelper.AddBlock( SystemGuid.Page.CHECK_IN_MAP_CONFIGURATION,
                null,
                "50029382-75A6-4B73-9644-880845B3116A" /* Check-in Types */,
                "Check-in Types",
                "Main",
                string.Empty,
                string.Empty,
                0,
                SystemGuid.Block.CONFIGURATION_CHECKIN_TYPES );

            RockMigrationHelper.AddBlock( SystemGuid.Page.CHECK_IN_MAP_CONFIGURATION,
                null,
                SystemGuid.BlockType.CHECKIN_MAP_DETAILS,
                "Checkin Map Details",
                "Main",
                string.Empty,
                string.Empty,
                1,
                SystemGuid.Block.CONFIGURATION_CHECKIN_MAP_DETAILS );

            RockMigrationHelper.AddBlock( SystemGuid.Page.PLUGIN_CONFIGURATION,
                null,
                Rock.SystemGuid.BlockType.HTML_CONTENT,
                "Content",
                "Main",
                string.Empty,
                string.Empty,
                0,
                SystemGuid.Block.PLUGIN_HTML );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.PLUGIN_HTML );
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.CONFIGURATION_CHECKIN_MAP_DETAILS );
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.CONFIGURATION_CHECKIN_TYPES );

            RockMigrationHelper.DeletePage( SystemGuid.Page.PLUGIN_CONFIGURATION );
            RockMigrationHelper.DeletePage( SystemGuid.Page.CHECK_IN_MAP_CONFIGURATION );
        }
    }
}
