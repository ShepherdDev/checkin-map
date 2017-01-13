using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Newtonsoft.Json;

using com.shepherdchurch.CheckinMap;

namespace RockWeb.Plugins.com_shepherdchurch.CheckinMap
{
    [DisplayName( "Checkin Map Kiosk" )]
    [Category( "com_shepherdchurch > Checkin Map" )]
    [Description( "Displays Checkin Map groups in a visual format that the user can interact with to find a serving opportunity." )]

    #region Block Attributes

    [TextField( "Header", "Message to display to the user in the page header.", true, "Select Your Serving Area", order: 0 )]
    [LinkedPage( "Serve Page", "The page to link the user to when they click on an available serving position. The group they click on is passed as 'groupId'.", false, order: 1 )]
    [GroupField( "Default Group", "The default group/area to display when the user first hits this page if nothing else is defined.", false, order: 2 )]
    [BooleanField( "Kiosk Mode", "Include the scripts to make working on a kiosk (touchscreen) more effective. Can cause problems for desktop browsers.", true, order: 3 )]
    [CodeEditorField( "Content Template", @"The lava that is run to generate the content. Rendered content is used for the text.
Assign the variables Title and CssClass to set the Title and CssClass to use for the content block.<br />
The following variables are defined:<br />
<ul>
 <li>Title - The title of the block, can be overridden.</li>
 <li>CssClass - The Css classes to use for the block, can be overridden.</li>
 <li>Item - Calculated information about the Group.
  <ul>
   <li>Type - 'Area' if this contains sub-groups or 'Position' if this is a group position to be filled.</li>
   <li>Minimum - Minimum number of spots that must be filled.</li>
   <li>Maximum - Maximum number of spots that may be filled.</li>
   <li>Have - Number of spots that have been filled.</li>
   <li>Need - Number of spots that need to be filled to meet the minimum (for an area this may not directly match Minimum - Have).</li>
   <li>Active - 'true' if check-in is currently active for this position.</li>
   <li>Group - The group object that represents this block.</li>
  </ul>
 </li>
</ul>", CodeEditorMode.Lava, defaultValue: @"Spots: {{ Item.Minimum }}{% if Item.Minimum != Item.Maximum %} - {{ Item.Maximum }}{% endif %}<br />Have: {{ Item.Have }}<br />Need: {{ Item.Need }}
{%- if Item.Active == 'false' -%}
 {%- assign CssClass = 'disabled' -%}
{%- elseif Item.Need > 0 -%}
 {%- assign CssClass = 'imagemap-danger' -%}
{%- elseif Item.Have < Item.Maximum -%}
 {%- assign CssClass = 'imagemap-warning' -%}
{%- else -%}
 {%- assign CssClass = 'imagemap-success' -%}
{%- endif -%}", order: 4 )]

    #endregion

    public partial class ServeInArea : RockBlock
    {
        #region Properties and Fields

        protected Group _group = null;
        int _defaultGroupId = 0;
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

            if ( GetAttributeValue( "KioskMode" ).AsBoolean(true) == true )
            {
                RockPage.AddScriptLink( "~/Scripts/iscroll.js" );
                RockPage.AddScriptLink( "~/Scripts/Kiosk/kiosk-core.js" );
            }

            RockPage.AddScriptLink( "~/Plugins/com_shepherdchurch/CheckinMap/Scripts/imagemap.js" );
            RockPage.AddCSSLink( "~/Plugins/com_shepherdchurch/CheckinMap/Styles/checkinmap.css" );

            imgImageMap.Attributes.Add( "onload", "imgImageMapLoaded()" );

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
            if ( !string.IsNullOrWhiteSpace( Request.QueryString["groupId"] ) && !string.IsNullOrWhiteSpace( Request.QueryString["json"] ) )
            {
                Group group = new GroupService( new RockContext() ).Get( Request.QueryString["groupId"].AsInteger() );
                List<ImageMapItem> items = new List<ImageMapItem>();

                foreach ( var grp in group.Groups )
                {
                    //
                    // Setup the default information for the Map Item.
                    //
                    items.Add( CheckinMapHelper.GetImageMapItemForGroup( RockPage, grp, GetAttributeValue( "ContentTemplate" ), GetUrlForGroup ) );
                }

                Response.Clear();
                Response.ContentType = "application/json";
                Response.Write( JsonConvert.SerializeObject( items ) );
                Response.End();

                return;
            }

            if ( !string.IsNullOrEmpty( PageParameter( "groupId" ) ) )
            {
                _defaultGroupId = PageParameter( "groupId" ).AsInteger();
            }
            else if ( !string.IsNullOrEmpty( GetAttributeValue( "DefaultGroup" ) ) )
            {
                _defaultGroupId = new GroupService( new RockContext() ).Get( GetAttributeValue( "DefaultGroup" ).AsGuid() ).Id;
            }

            //
            // Set default group if this is not a postback.
            //
            if ( !IsPostBack )
            {
                hfMapData.Value = Convert.ToBase64String( Encoding.UTF8.GetBytes( "[]" ) );
                hfGroupId.Value = _defaultGroupId.ToString();
            }

            _group = new GroupService( new RockContext() ).Get( hfGroupId.Value.AsInteger() );

            //
            // Display the current group if this is not a postback.
            //
            if ( !IsPostBack )
            {
                DisplayGroup( _group );
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

        /// <summary>
        /// Build the JSON data to draw the image map for this group and all it's children.
        /// </summary>
        /// <param name="group">The group to display as an Image Map.</param>
        void DisplayGroup( Group group )
        {
            hHeader.InnerText = GetAttributeValue( "Header" );

            imgImageMap.Visible = false;
            lbBack.AddCssClass( "invisible" );

            if ( group != null )
            {
                group.LoadAttributes();
                if ( !string.IsNullOrWhiteSpace( group.GetAttributeValue( "Background" ) ) )
                {
                    imgImageMap.Src = string.Format( "{0}?guid={1}", System.Web.VirtualPathUtility.ToAbsolute( "~/GetImage.ashx" ), group.GetAttributeValue( "Background" ) );
                    imgImageMap.Visible = true;

                    List<ImageMapItem> items = new List<ImageMapItem>();
                    foreach ( var grp in group.Groups )
                    {
                        //
                        // Setup the default information for the Map Item.
                        //
                        items.Add( CheckinMapHelper.GetImageMapItemForGroup( RockPage, grp, GetAttributeValue( "ContentTemplate" ), GetUrlForGroup ) );
                    }

                    hfMapData.Value = Convert.ToBase64String( Encoding.UTF8.GetBytes( JsonConvert.SerializeObject( items ) ) );
                }

                if ( group.ParentGroupId.HasValue && group.Id != _defaultGroupId )
                {
                    lbBack.RemoveCssClass( "invisible" );
                }
            }
        }

        /// <summary>
        /// Get the URL for the given group. This is the URL that will be used when the user clicks
        /// on a group element on the image map.
        /// </summary>
        /// <param name="group">The group to be linked to.</param>
        /// <param name="isPosition">true if this group is a serving position, false if it is a sub-area.</param>
        /// <returns>A string that represents the contents of the href hyperlink for the group.</returns>
        string GetUrlForGroup( Group group, bool isPosition )
        {
            if ( isPosition )
            {
                if ( !string.IsNullOrEmpty( GetAttributeValue( "ServePage" ) ) )
                {
                    PageReference pageRef = new PageReference( GetAttributeValue( "ServePage" ) );

                    pageRef.Parameters = new Dictionary<string, string> { { "groupId", group.Id.ToString() } };
                    return pageRef.BuildUrl();
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                //
                // This block itself does not do check-in, but it has child-groups to display.
                // This will cause a postback so we can redraw with the selected group.
                //
                _registerCallbacks.Add( new KeyValuePair<string, string>( lbSelectGroup.UniqueID, group.Id.ToString() ) );
                return Page.ClientScript.GetPostBackClientHyperlink( lbSelectGroup, group.Id.ToString() );
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
            this.NavigateToCurrentPage();
        }

        /// <summary>
        /// Handles the Click event of the control. Go the parent group if that is possible.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbBack_Click( object sender, EventArgs e )
        {
            hfGroupId.Value = _group.ParentGroupId.ToString();

            DisplayGroup( _group.ParentGroup );
        }

        /// <summary>
        /// Helper method to handle when the user clicks one of the dynamically generated group blocks.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSelectGroup_Command( object sender, CommandEventArgs e )
        {
            hfGroupId.Value = Request.Form["__EVENTARGUMENT"];

            DisplayGroup( new GroupService( new RockContext() ).Get( hfGroupId.Value.AsInteger() ) );
        }

        #endregion
    }
}
