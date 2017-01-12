using System;
using System.ComponentModel;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_shepherdchurch.ServingMap
{
    [DisplayName( "Group Attendance Record From Url" )]
    [Category( "com_shepherdchurch > Serving Map" )]
    [Description( "Takes a personId and groupId from the URL and marks the person as attended in that group." )]

    [BooleanField( "Use Current Person", "Use the currently logged in person if not specified in the query string.", false, order: 0 )]
    [LinkedPage( "Redirect Page", "The page to redirect the user to once the attendance has been recorded.", false, "", "", 1 )]
    [CodeEditorField( "Message", "Message to display to the user if the Redirect Page setting is not set. The Lava variables Person and Group are available.", CodeEditorMode.Lava, required: false, order: 2 )]
    public partial class GroupAttendanceRecordFromUrl : RockBlock
    {
        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Initialize basic information about the page structure and setup the default content.
        /// </summary>
        /// <param name="sender">Object that is generating this event.</param>
        /// <param name="e">Arguments that describe this event.</param>
        protected void Page_Load( object sender, EventArgs e )
        {
            if ( !IsPostBack )
            {
                if ( string.IsNullOrEmpty( GetAttributeValue( "RedirectPage" ) ) && string.IsNullOrEmpty( GetAttributeValue( "Lava" ) ) )
                {
                    nbWarning.Text = "Block has not been configured.";
                    return;
                }

                if ( !string.IsNullOrWhiteSpace( PageParameter( "personId" ) ) )
                {
                    RecordAttendance( PageParameter( "groupId" ).AsInteger(), PageParameter( "personId" ).AsInteger() );
                }
                else
                {
                    if ( GetAttributeValue( "UseCurrentPerson" ).AsBoolean() == true && CurrentPerson != null )
                    {
                        RecordAttendance( PageParameter( "groupId" ).AsInteger(), CurrentPerson.Id );
                    }
                    else
                    {
                        nbWarning.Text = "Could not identify the person to be marked as attended.";
                        return;
                    }
                }
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Mark the person as attended for the given group.
        /// </summary>
        /// <param name="groupId">The Id of the group to take attendance for.</param>
        /// <param name="personId">The Id of the person to take attendance for.</param>
        void RecordAttendance( int groupId, int personId )
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            PersonService personService = new PersonService( rockContext );
            AttendanceService attendanceService = new AttendanceService( rockContext );
            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            LocationService locationService = new LocationService( rockContext );
            DateTime date = RockDateTime.Now;
            Group group;
            Person person;
            int? campusId = null;
            int? scheduleId = null;
            int? locationId = null;

            group = groupService.Get( groupId );
            person = personService.Get( personId );

            if ( group != null && group.Id != 0 && person != null && person.Id != 0 )
            {
                var primaryAlias = personAliasService.GetPrimaryAlias( personId );

                //
                // Determine if this is a check-in style attendance or just regular group attendance.
                //
                GroupLocation groupLocation = group.GroupLocations
                    .Where( gl => gl.Schedules.Where( s => s.IsCheckInActive ).Any() )
                    .FirstOrDefault();
                if ( groupLocation != null )
                {
                    campusId = locationService.Get( groupLocation.Location.Id ).CampusId;
                    scheduleId = groupLocation.Schedules.Where( s => s.IsCheckInActive ).First().Id;
                    locationId = groupLocation.Location.Id;
                }

                //
                // Get an existing attendance record or create a new one.
                //
                var attendance = attendanceService.Get( date, ( locationId ?? 0 ), ( scheduleId ?? 0 ), group.Id, personId );
                if ( attendance == null )
                {
                    attendance = rockContext.Attendances.Create();
                    attendance.LocationId = locationId;
                    attendance.CampusId = campusId;
                    attendance.ScheduleId = scheduleId;
                    attendance.GroupId = group.Id;
                    attendance.PersonAlias = primaryAlias;
                    attendance.PersonAliasId = primaryAlias.Id;
                    attendanceService.Add( attendance );
                }

                attendance.StartDateTime = date;
                attendance.EndDateTime = null;
                attendance.DidAttend = true;

                if ( attendance.LocationId.HasValue )
                {
                    Rock.CheckIn.KioskLocationAttendance.Flush( attendance.LocationId.Value );
                }

                rockContext.SaveChanges();

                if ( !string.IsNullOrEmpty( GetAttributeValue( "RedirectPage" ) ) )
                {
                    Redirect( new PageReference( GetAttributeValue( "RedirectPage" ) ).BuildUrl() );
                }
                else
                {
                    string template = GetAttributeValue( "Message" );
                    var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage, CurrentPerson );

                    mergeFields.Add( "Group", group );
                    mergeFields.Add( "Person", person );

                    ltContent.Text = template.ResolveMergeFields( mergeFields );
                }
            }
            else
            {
                nbWarning.Text = "Could not identify either the person or the group to record attendance.";
            }
        }

        /// <summary>
        /// Redirect the user. Since this is automatic give Administrators a chance to override.
        /// </summary>
        /// <param name="url">URL to send the user to.</param>
        void Redirect( string url )
        {
            if ( UserCanAdministrate )
            {
                nbWarning.Text = string.Format(
                    "If you were not an Administrator you would have been redirected to <a href=\"{0}\">{0}</a>.",
                    url );
            }
            else
            {
                Response.Redirect( url );
                Response.End();
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnDone_Click( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "RedirectPage" );
        }

        #endregion
    }
}