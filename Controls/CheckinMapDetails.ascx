<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CheckinMapDetails.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.CheckinMap.CheckinMapDetails" %>

<style>
    .checkin-item {
        padding: 12px;
        border: 1px solid #d8d1c8;
        cursor: default;
        margin-bottom: 6px;
        border-top-width: 3px;
    }
    .checkin-item-selected {
        background-color: #d8d1c8;
    }
    .checkin-list {
        list-style-type: none;
        padding-left: 20px;
    }
    .checkin-list-first {
        padding-left: 0;
    }
    
    .checkin-group {
        border-top-color: #afd074;
        cursor: pointer;
    }

    .checkin-area {
        border-top-color: #5593a4;
    }

.panel-details > div:first-child {
    text-align: center;
    margin-bottom: 20px;
}
</style>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfGroupGuid" runat="server" />
        <asp:HiddenField ID="hfScrollToDetails" runat="server" />
        <asp:Panel ID="pnlDetails" runat="server" CssClass="panel panel-block">
            <div class="panel-heading"><h3 class="panel-title"><i class="fa fa-list"></i> Areas and Groups</h3></div>
            <div class="panel-body">
                <div class="row">
                    <div class="col-md-4">
                        <ul class="checkin-list checkin-list-first">
                            <asp:PlaceHolder ID="phRows" runat="server" />
                        </ul>
                    </div>

                    <div class="col-md-8 js-panel-details">
                        <Rock:NotificationBox ID="nbDetailMessage" runat="server" NotificationBoxType="Warning" Visible="false" />
                        <asp:Panel ID="pnlImageMap" runat="server" Visible="false" CssClass="panel-details">
                            <div>
                                <img id="imgImageMap" runat="server" style="max-width: 100%;" src="." />
                            </div>
                            <asp:Button ID="btnSave" runat="server" CssClass="btn btn-primary" Text="Save" />
                        </asp:Panel>
                    </div>
                </div>
            </div>
        </asp:Panel>

        <asp:HiddenField ID="hfMapData" runat="server" />
        <script>
            var Base64 = { _keyStr: "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=", encode: function (e) { var t = ""; var n, r, i, s, o, u, a; var f = 0; e = Base64._utf8_encode(e); while (f < e.length) { n = e.charCodeAt(f++); r = e.charCodeAt(f++); i = e.charCodeAt(f++); s = n >> 2; o = (n & 3) << 4 | r >> 4; u = (r & 15) << 2 | i >> 6; a = i & 63; if (isNaN(r)) { u = a = 64 } else if (isNaN(i)) { a = 64 } t = t + this._keyStr.charAt(s) + this._keyStr.charAt(o) + this._keyStr.charAt(u) + this._keyStr.charAt(a) } return t }, decode: function (e) { var t = ""; var n, r, i; var s, o, u, a; var f = 0; e = e.replace(/[^A-Za-z0-9+/=]/g, ""); while (f < e.length) { s = this._keyStr.indexOf(e.charAt(f++)); o = this._keyStr.indexOf(e.charAt(f++)); u = this._keyStr.indexOf(e.charAt(f++)); a = this._keyStr.indexOf(e.charAt(f++)); n = s << 2 | o >> 4; r = (o & 15) << 4 | u >> 2; i = (u & 3) << 6 | a; t = t + String.fromCharCode(n); if (u != 64) { t = t + String.fromCharCode(r) } if (a != 64) { t = t + String.fromCharCode(i) } } t = Base64._utf8_decode(t); return t }, _utf8_encode: function (e) { e = e.replace(/rn/g, "n"); var t = ""; for (var n = 0; n < e.length; n++) { var r = e.charCodeAt(n); if (r < 128) { t += String.fromCharCode(r) } else if (r > 127 && r < 2048) { t += String.fromCharCode(r >> 6 | 192); t += String.fromCharCode(r & 63 | 128) } else { t += String.fromCharCode(r >> 12 | 224); t += String.fromCharCode(r >> 6 & 63 | 128); t += String.fromCharCode(r & 63 | 128) } } return t }, _utf8_decode: function (e) { var t = ""; var n = 0; var r = c1 = c2 = 0; while (n < e.length) { r = e.charCodeAt(n); if (r < 128) { t += String.fromCharCode(r); n++ } else if (r > 191 && r < 224) { c2 = e.charCodeAt(n + 1); t += String.fromCharCode((r & 31) << 6 | c2 & 63); n += 2 } else { c2 = e.charCodeAt(n + 1); c3 = e.charCodeAt(n + 2); t += String.fromCharCode((r & 15) << 12 | (c2 & 63) << 6 | c3 & 63); n += 3 } } return t } }
            /* This function is called after post back to animate scroll to the proper element 
             * if the user just clicked an area/group.
            */
            var AfterPostBack = function () {
                if ($('#<%= hfScrollToDetails.ClientID %>').val() == "true" && $('.js-panel-details').length) {
                    $('#<%= hfScrollToDetails.ClientID %>').val("false");
                    $('html, body').animate({
                        scrollTop: $(".js-panel-details").offset().top - 8 + 'px'
                        }, 400
                    );
                }
            }
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(AfterPostBack);
            Sys.Application.add_load(function ()
            {
                $('#<%= btnSave.ClientID %>').click(function (e)
                {
                    var data = Base64.encode(JSON.stringify($('#<%= imgImageMap.ClientID %>').ImageMap('getActions')));
                    __doPostBack('<%= upnlContent.ClientID %>', 'save:' + data);
                    e.preventDefault();
                });

                $('section.checkin-item').click(function ()
                {
                    if (true /*!isDirty()*/)
                    {
                        var $li = $(this).closest('li');
                        if ($(this).hasClass('checkin-group'))
                        {
                            __doPostBack('<%= upnlContent.ClientID %>', 'select-group:' + $li.attr('data-key'));
                        }
                    }
                });

                if ($('#<%= imgImageMap.ClientID %>').length > 0)
                {
                    try
                    {
                        var actions = JSON.parse(Base64.decode($('#<%= hfMapData.ClientID %>').val()));
                        $('#<%= imgImageMap.ClientID %>').ImageMap({ edit: <%= UserCanEdit.ToString().ToLower() %>, actions: actions });
                        $('#<%= hfMapData.ClientID %>').val('');
                    }
                    catch (e) { }
                }
            });
        </script>
    </ContentTemplate>
</asp:UpdatePanel>
