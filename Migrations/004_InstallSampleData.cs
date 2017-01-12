using System;
using System.Collections.Generic;

using Rock.Plugin;

namespace com.shepherdchurch.CheckinMap.Migrations
{
    [MigrationNumber( 4, "1.5.0" )]
    public class InstallSampleData : CheckinMapMigration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            Guid checkinMapKioskServePage = Guid.NewGuid();
            Guid checkinMapKioskKioskMode = Guid.NewGuid();
            Guid recordAttendanceUseCurrentPerson = Guid.NewGuid();
            Guid recordAttendanceRedirectPage = Guid.NewGuid();

            /* Configure BlockType Attributes */
            RockMigrationHelper.UpdateBlockTypeAttribute( SystemGuid.BlockType.CHECKIN_MAP_KIOSK,
                "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108",
                "Serve Page",
                "ServePage",
                string.Empty,
                "The page to link the user to when they click on an available serving position. The group they click on is passed as 'groupId'.",
                1,
                string.Empty,
                checkinMapKioskServePage.ToString() );

            RockMigrationHelper.UpdateBlockTypeAttribute( SystemGuid.BlockType.CHECKIN_MAP_KIOSK,
                "1EDAFDED-DFE6-4334-B019-6EECBA89E05A",
                "Kiosk Mode",
                "KioskMode",
                string.Empty,
                "Include the scripts to make working on a kiosk (touchscreen) more effective. Can cause problems for desktop browsers.",
                3,
                "true",
                checkinMapKioskKioskMode.ToString() );

            RockMigrationHelper.UpdateBlockTypeAttribute( SystemGuid.BlockType.GROUP_ATTENDANCE_RECORD_FROM_URL,
                "1EDAFDED-DFE6-4334-B019-6EECBA89E05A",
                "Use Current Person",
                "UseCurrentPerson",
                string.Empty,
                "Use the currently logged in person if not specified in the query string.",
                0,
                "false",
                recordAttendanceUseCurrentPerson.ToString() );

            RockMigrationHelper.UpdateBlockTypeAttribute( SystemGuid.BlockType.GROUP_ATTENDANCE_RECORD_FROM_URL,
                "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108",
                "Redirect Page",
                "RedirectPage",
                string.Empty,
                "The page to redirect the user to once the attendance has been recorded.",
                1,
                "",
                recordAttendanceRedirectPage.ToString() );

            /* Configure Pages */
            RockMigrationHelper.AddPage( SystemGuid.Page.PLUGIN_CONFIGURATION,
                "D65F783D-87A9-4CC9-8110-E83466A0EADB" /* Full Width Layout */,
                "Check-in Map",
                "A sample page for viewing map groups.",
                SystemGuid.Page.PLUGIN_SAMPLE_MAP,
                "" );
            int sampleMapPageId = ( int ) SqlScalar( "SELECT [Id] FROM [Page] WHERE [Guid] = @Guid",
                new Dictionary<string, object> { { "@Guid", SystemGuid.Page.PLUGIN_SAMPLE_MAP } } );

            RockMigrationHelper.AddPage( SystemGuid.Page.PLUGIN_CONFIGURATION,
                "D65F783D-87A9-4CC9-8110-E83466A0EADB" /* Full Width Layout */,
                "Record Attendance",
                "A sample page for recording attendance in the group selected on the map.",
                SystemGuid.Page.PLUGIN_SAMPLE_ATTENDANCE,
                "" );

            /* Configure Blocks */
            RockMigrationHelper.AddBlock( SystemGuid.Page.PLUGIN_SAMPLE_MAP,
                null,
                SystemGuid.BlockType.CHECKIN_MAP_KIOSK,
                "Check-in Map",
                "Main",
                string.Empty,
                string.Empty,
                0,
                SystemGuid.Block.PLUGIN_SAMPLE_CHECKIN_MAP_KIOSK );

            RockMigrationHelper.AddBlock( SystemGuid.Page.PLUGIN_SAMPLE_ATTENDANCE,
                null,
                SystemGuid.BlockType.GROUP_ATTENDANCE_RECORD_FROM_URL,
                "Record Attendance",
                "Main",
                string.Empty,
                string.Empty,
                0,
                SystemGuid.Block.PLUGIN_SAMPLE_RECORD_ATTENDANCE );

            /* Configure Block Attribute Values */
            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.PLUGIN_SAMPLE_CHECKIN_MAP_KIOSK,
                checkinMapKioskServePage.ToString(),
                SystemGuid.Block.PLUGIN_SAMPLE_RECORD_ATTENDANCE );

            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.PLUGIN_SAMPLE_CHECKIN_MAP_KIOSK,
                checkinMapKioskKioskMode.ToString(),
                "false" );

            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.PLUGIN_SAMPLE_RECORD_ATTENDANCE,
                recordAttendanceUseCurrentPerson.ToString(),
                "true" );

            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.PLUGIN_SAMPLE_RECORD_ATTENDANCE,
                recordAttendanceRedirectPage.ToString(),
                SystemGuid.Page.PLUGIN_CONFIGURATION );

            /* HTML Content */
            RockMigrationHelper.UpdateHtmlContentBlock( SystemGuid.Block.PLUGIN_HTML,
                String.Format(@"<h1>Group Setup</h1>

<p>We are going to walk through setting up a check-in map for Ushers with 3 positions available to serve.</p>

<p>First you need to go into the normal <code>Admin Tools -> Check-in</code> screen and open the <code>Check-in Configuration</code> page.
Select the <code>Volunteer Check-in Area</code> and under the default <code>Serving Team</code> area click the <i>Add Area</i> button and name
it <code>Usher Team</code>. Also change the <code>Inherit from</code> option to <code>Check in Map</code>.</p>

<p>Now, under the <code>Usher Team</code> item, click the Add Group button and name this one <code>Sanctuary</code>. You will need to
upload an image to use into the <code>Background</code> attribute. For now it can be the sample image below but you would want this to
be an image of the floor plan of your Sanctuary. Ignore the <code>Need</code> and <code>Position</code> attributes as they are not used for
this group.</p>

<p>Next, under the new <code>Sanctuary</code> item, click the <i>Add Group</i> button and name this one <code>Left Aisle</code>. Set the
<code>Need</code> values to <code>2 - 4</code>. Leave the <code>Background</code> and <code>Position</code> attributes empty. Repeat this
step to create two more items under <code>Sanctuary</code>, named <code>Center Aisle</code> and <code>Right Aisle</code>. This should give
you 3 positions, each needing between 2 and 4 people to fill each of those roles.</p>

<p>Go back to the <code>Admin Tools -> Check-in</code> screen and now open the <code>Check-in Map Configuration</code> page. Select the
<code>Volunteer Check-in Area</code> and then select the <code>Sanctuary</code> you just created. You should see the image you uploaded
as well as a few buttons (they might be stacked on top of each other). Use the mouse to click and drag those
buttons where you want them appear on the image. Once you have them positioned (this fills in the <code>Position</code> attributes
for you by the way) click the <i>Save</i> button.</p>

<h2>Test Your Groups</h2>

<p>You will notice in the <code>Check-in Map Configuration</code> page that there are numbers next to the name of each group. These are the group
ID numbers, which you need to be able to pass to the kiosk block. There are sample kiosk pages as children of this page that you may
look at for reference on how to create your own page structure. For now you can test this by entering the group ID of the <code>Sanctuary</code>
group into the box below and clicking the <i>Test</i> button.</p>

<p><input type=""text"" id=""checkinMapTestGroupId"" placeholder=""Group Id"" class=""form-control"" /><br />
<a href=""/page/{0}"" id=""btnCheckinMapTest"" class=""btn btn-primary"">Test</a></p>

<script type=""text/javascript"">
$(document).ready(function () {{
  $( '#btnCheckinMapTest' ).click( function( e ) {{
    e.preventDefault();
    window.location = $( this ).attr( 'href' ) + '?groupId=' + $( '#checkinMapTestGroupId' ).val();
  }});
}});
</script>

<h1>Page Structure</h1>

<p>The sample page structure simply allows the currently logged in user to select a serving opportunity and then sends them to another page
which records their attendance for that group. But this really becomes useful when the user does not have to log in first.There is another
block in a separate plugin that can be paired with these blocks to give that ability. The<code> Self Serve Search</code> block provies the
user with a search function very similar to self-serve check-in. After the user selects their person from the list it can be configured to
redirect the user to the same <code>Group Attendance Record From Url</code> block we use which will then take the<code> groupId</code> from
the URL as well as the<code> personId</code> from the URL and record attendance for the person that was searched for and selected.</p>

<p>Other structures can be defined as well.One idea might be an information inquiry about your serving opportunities available. Instead of
redirecting the user to an attendance record page, redirect them to a page that automatically starts a workflow based on the values from
the URL.</p>

<h1>Sample Floorplan</h1>

<p><img src=""~/Plugins/com_shepherdchurch/CheckinMap/Samples/Floor.png"" style=""max-width: 100%;"" /></p>", sampleMapPageId),
                Guid.NewGuid().ToString() );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.PLUGIN_SAMPLE_RECORD_ATTENDANCE );
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.PLUGIN_SAMPLE_CHECKIN_MAP_KIOSK );
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.CONFIGURATION_CHECKIN_TYPES );

            RockMigrationHelper.DeletePage( SystemGuid.Page.PLUGIN_SAMPLE_ATTENDANCE );
            RockMigrationHelper.DeletePage( SystemGuid.Page.PLUGIN_SAMPLE_MAP );
        }
    }
}
