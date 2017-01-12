using System;
using System.Collections.Generic;

using Rock.Plugin;

namespace com.shepherdchurch.CheckinMap.Migrations
{
    [MigrationNumber( 1, "1.5.0" )]
    public class InstallGroupTypes : CheckinMapMigration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            int checkinMapId;
            int count;

            RockMigrationHelper.UpdateGroupType( "Check in Map",
                "A base group type that can be inherited from to support the visual check-in system.",
                "Group",
                "Member",
                null,
                false,
                false,
                false,
                "fa fa-map",
                1500,
                null,
                0,
                Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_FILTER,
                SystemGuid.GroupType.CHECKIN_MAP,
                true );

            RockMigrationHelper.AddGroupTypeGroupAttribute( SystemGuid.GroupType.CHECKIN_MAP,
                Rock.SystemGuid.FieldType.IMAGE,
                "Background",
                "The background image to use when displaying the map of child-groups.",
                0,
                null,
                Guid.NewGuid().ToString() );

            RockMigrationHelper.AddGroupTypeGroupAttribute( SystemGuid.GroupType.CHECKIN_MAP,
                Rock.SystemGuid.FieldType.DECIMAL,
                "Position X",
                "The x-position of this group within it's parent group, specified as a percentage from 0 - 100.",
                0,
                null,
                Guid.NewGuid().ToString() );

            RockMigrationHelper.AddGroupTypeGroupAttribute( SystemGuid.GroupType.CHECKIN_MAP,
                Rock.SystemGuid.FieldType.DECIMAL,
                "Position Y",
                "The y-position of this group within it's parent group, specified as a percentage from 0 - 100.",
                0,
                null,
                Guid.NewGuid().ToString() );

            RockMigrationHelper.AddGroupTypeGroupAttribute( SystemGuid.GroupType.CHECKIN_MAP,
                Rock.SystemGuid.FieldType.INTEGER_RANGE,
                "Need",
                "The minimum number of volunteers for this position to be considered filled as well as the maximum number that can serve in this position.",
                0,
                null,
                Guid.NewGuid().ToString() );

            //
            // Get the ID number(s) of the item(s) we just created.
            //
            checkinMapId = ( int ) SqlScalar( "SELECT [Id] FROM [GroupType] WHERE [Guid] = @Guid",
                new Dictionary<string, object> { { "@Guid", SystemGuid.GroupType.CHECKIN_MAP } } );

            //
            // Setup the allowed sub-types.
            //
            count = ( int ) SqlScalar( "SELECT COUNT(*) FROM [GroupTypeAssociation] WHERE [GroupTypeId] = @Parent AND [ChildGroupTypeId] = @Child",
                new Dictionary<string, object> { { "@Parent", checkinMapId }, { "@Child", checkinMapId } } );
            if ( count == 0 )
            {
                Sql( "INSERT INTO [GroupTypeAssociation] ([GroupTypeId], [ChildGroupTypeId]) VALUES (@Parent, @Child)",
                    new Dictionary<string, object> { { "@Parent", checkinMapId }, { "@Child", checkinMapId } } );
            }
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            Sql( "UPDATE [GroupType] SET [IsSystem] = 0 WHERE [Guid] = @Guid",
                new Dictionary<string, object> { { "@Guid", SystemGuid.GroupType.CHECKIN_MAP } } );
        }
    }
}
