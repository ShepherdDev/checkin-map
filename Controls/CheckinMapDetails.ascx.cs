using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

using com.shepherdchurch.CheckinMap;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_shepherdchurch.CheckinMap
{
    [DisplayName( "Checkin Map Details" )]
    [Category( "com_shepherdchurch > Checkin Map" )]
    [Description( "Shows and edits details of a check-in type to be used as a visual map." )]

    public partial class CheckinMapDetails : RockBlock
    {
        #region Properties and Fields

        private GroupType _checkinType = null;
        private List<Guid> _groupTypes = new List<Guid>();
        private List<Guid> _groups = new List<Guid>();
        private Guid? _currentGroupGuid = null;

        protected Group _group = null;
        List<KeyValuePair<string, string>> _registerCallbacks;

        #endregion

        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            _registerCallbacks = new List<KeyValuePair<string, string>>();

            RockPage.AddScriptLink( "~/Scripts/iscroll.js" );
            RockPage.AddScriptLink( "~/Scripts/Kiosk/kiosk-core.js" );
            RockPage.AddScriptLink( "~/Plugins/com_shepherdchurch/CheckinMap/Scripts/imagemap.js" );
            RockPage.AddCSSLink( "~/Plugins/com_shepherdchurch/CheckinMap/Styles/checkinmap.css" );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Initialize basic information about the page structure and setup the default content.
        /// </summary>
        /// <param name="sender">Object that is generating this event.</param>
        /// <param name="e">Arguments that describe this event.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( hfGroupGuid.Value.AsGuid() != Guid.Empty )
            {
                _currentGroupGuid = hfGroupGuid.Value.AsGuid();
                _group = new GroupService( new RockContext() ).Get( hfGroupGuid.Value.AsGuid() );
            }

            BuildRows( !Page.IsPostBack );

            if ( _checkinType == null )
            {
                pnlDetails.Visible = false;
            }
            else
            {
                pnlDetails.Visible = true;

                if ( Page.IsPostBack )
                {
                    ProcessCustomEvents();
                }
            }
        }

        /// <summary>
        /// Override the Render method so we can register for event validation.
        /// </summary>
        /// <param name="writer">The HtmlTextWriter object that receives server control content.</param>
        protected override void Render( HtmlTextWriter writer )
        {
            base.Render( writer );

            foreach ( var kvp in _registerCallbacks )
            {
                Page.ClientScript.RegisterForEventValidation( kvp.Key, kvp.Value );
            }
        }

        #endregion

        #region Core Methods

        private void BuildRows( bool setValues = true )
        {
            _groupTypes.Clear();
            _groups.Clear();
            phRows.Controls.Clear();

            using ( var rockContext = new RockContext() )
            {
                _checkinType = new GroupTypeService( rockContext ).Get( PageParameter( "CheckInTypeId" ).AsInteger() );
                if ( _checkinType != null )
                {
                    foreach ( var groupType in _checkinType.ChildGroupTypes
                        .Where( t => t.Id != _checkinType.Id )
                        .OrderBy( t => t.Order )
                        .ThenBy( t => t.Name ) )
                    {
                        BuildCheckinAreaRow( groupType, phRows, setValues );
                    }
                }
            }
        }

        private void BuildCheckinAreaRow( GroupType groupType, Control parentControl, bool setValues )
        {
            if ( groupType != null && !_groupTypes.Contains( groupType.Guid ) &&
                ( groupType.GroupTypePurposeValue == null || !groupType.GroupTypePurposeValue.Guid.Equals( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_FILTER.AsGuid() ) ) )
            {
                _groupTypes.Add( groupType.Guid );

                HtmlGenericControl li = new HtmlGenericControl( "li" );
                HtmlGenericControl section = new HtmlGenericControl( "section" );
                HtmlGenericControl div = new HtmlGenericControl( "div" );
                HtmlGenericControl ul = new HtmlGenericControl( "ul" );
                parentControl.Controls.Add( li );
                li.Controls.Add( section );
                div.Controls.Add( ul );

                section.InnerText = string.Format( "{0} <span class=\"label label-info\"#{1}</span>", groupType.Name, groupType.Id );
                section.InnerText = groupType.Name;
                section.Attributes.Add( "class", "checkin-item checkin-area" );

                foreach ( var childGroupType in groupType.ChildGroupTypes
                    .Where( t => t.Id != groupType.Id )
                    .OrderBy( t => t.Order )
                    .ThenBy( t => t.Name ) )
                {
                    BuildCheckinAreaRow( childGroupType, ul, setValues );
                }

                // Find the groups of this type, who's parent is null, or another group type ( "root" groups ).
                var allGroupIds = groupType.Groups.Select( g => g.Id ).ToList();
                foreach ( var childGroup in groupType.Groups
                    .Where( g =>
                        !g.ParentGroupId.HasValue ||
                        !allGroupIds.Contains( g.ParentGroupId.Value ) )
                    .OrderBy( a => a.Order )
                    .ThenBy( a => a.Name ) )
                {
                    BuildCheckinGroupRow( childGroup, ul, setValues );
                }

                if ( ul.Controls.Count > 0 )
                {
                    ul.Attributes.Add( "class", "checkin-list" );
                    li.Controls.Add( div );
                }
            }
        }

        private void BuildCheckinGroupRow( Group group, Control parentControl, bool setValues )
        {
            if ( group != null && !_groups.Contains( group.Guid ) )
            {
                HtmlGenericControl li = new HtmlGenericControl( "li" );
                HtmlGenericControl section = new HtmlGenericControl( "section" );
                HtmlGenericControl div = new HtmlGenericControl( "div" );
                HtmlGenericControl ul = new HtmlGenericControl( "ul" );
                parentControl.Controls.Add( li );
                li.Controls.Add( section );
                div.Controls.Add( ul );

                section.InnerText = string.Format( "{0} <span class=\"label label-info\"#{1}</span>", group.Name, group.Id );
                section.Attributes.Add( "class", "checkin-item" );
                section.AddCssClass( "checkin-group " );
                li.Attributes.Add( "data-key", group.Guid.ToString() );

                if ( _currentGroupGuid.HasValue && group.Guid.Equals( _currentGroupGuid.Value ) )
                {
                    section.AddCssClass( "checkin-item-selected" );
                }

                foreach ( var childGroup in group.Groups
                    .Where( g => g.GroupTypeId == group.GroupTypeId )
                    .OrderBy( a => a.Order )
                    .ThenBy( a => a.Name ) )
                {
                    BuildCheckinGroupRow( childGroup, ul, setValues );
                }

                if ( ul.Controls.Count > 0 )
                {
                    ul.Attributes.Add( "class", "checkin-list" );
                    li.Controls.Add( div );
                }
            }
        }

        /// <summary>
        /// Build the JSON data to draw the image map for this group and all it's children.
        /// </summary>
        /// <param name="group">The group to display as an Image Map.</param>
        void DisplayGroup( Group group )
        {
            string contentTemplate = "{%- assign CssClass = 'imagemap-success' -%}<br />";

            group.LoadAttributes();
            if ( !string.IsNullOrWhiteSpace( group.GetAttributeValue( "Background" ) ) )
            {
                imgImageMap.Visible = true;
                btnSave.Visible = UserCanEdit;
                imgImageMap.Src = string.Format( "{0}?guid={1}", VirtualPathUtility.ToAbsolute( "~/GetImage.ashx" ), group.GetAttributeValue( "Background" ) );

                IEnumerable<ImageMapItem> items = CheckinMapHelper.GetImapeMapItemsForParentGroupId( group.Id, RockPage, contentTemplate, null );

                hfMapData.Value = Convert.ToBase64String( Encoding.UTF8.GetBytes( JsonConvert.SerializeObject( items ) ) );
                nbDetailMessage.Visible = false;
            }
            else
            {
                imgImageMap.Visible = false;
                hfMapData.Value = Convert.ToBase64String( Encoding.UTF8.GetBytes( "[]" ) );
                btnSave.Visible = false;

                nbDetailMessage.Text = "Group does not have a background set, cannot configure.";
                nbDetailMessage.Visible = true;
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
            if ( _group != null )
            {
                DisplayGroup( _group );
            }
        }

        private void ProcessCustomEvents()
        {
            string postbackArgs = Request.Params["__EVENTARGUMENT"];
            if ( !string.IsNullOrWhiteSpace( postbackArgs ) )
            {
                string[] nameValue = postbackArgs.Split( new char[] { ':' } );
                if ( nameValue.Length == 2 )
                {
                    string eventParam = nameValue[0];
                    switch ( eventParam )
                    {
                        case "select-group":
                            {
                                SelectGroup( nameValue[1].AsGuid() );
                                break;
                            }

                        case "save":
                            {
                                DoSave( nameValue[1] );
                                break;
                            }
                    }
                }
            }
        }

        private void SelectGroup( Guid? groupGuid )
        {
            nbDetailMessage.Text = string.Empty;
            nbDetailMessage.Visible = false;
            pnlImageMap.Visible = false;

            if ( groupGuid.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    GroupService groupService = new GroupService( rockContext );
                    Group group = groupService.Get( groupGuid.Value );

                    if ( group != null )
                    {
                        _currentGroupGuid = group.Guid;
                        hfGroupGuid.Value = group.Guid.ToString();
                        pnlImageMap.Visible = true;
                        DisplayGroup( group );
                    }
                    else
                    {
                        _currentGroupGuid = null;
                    }
                }
            }
            else
            {
                _currentGroupGuid = null;
            }

            BuildRows();

            hfScrollToDetails.Value = "true";
        }

        protected void DoSave( string base64Data )
        {
            string json = Encoding.UTF8.GetString( Convert.FromBase64String( base64Data ) );
            var data = JsonConvert.DeserializeObject<List<ImageMapItem>>( json );

            using ( var rockContext = new RockContext() )
            {
                GroupService groupService = new GroupService( rockContext );

                foreach ( var item in data )
                {
                    Group group = groupService.Get( item.Identifier.AsGuid() );

                    if ( group != null )
                    {
                        group.LoadAttributes( rockContext );
                        group.SetAttributeValue( "PositionX", item.PositionX );
                        group.SetAttributeValue( "PositionY", item.PositionY );
                        group.SaveAttributeValues( rockContext );
                    }
                }

                rockContext.SaveChanges();

                DisplayGroup( new GroupService( rockContext ).Get( hfGroupGuid.Value.AsGuid() ) );
                hfScrollToDetails.Value = "true";

                nbDetailMessage.Text = "Your changes have been saved.";
                nbDetailMessage.Visible = true;
            }
        }

        #endregion
    }
}