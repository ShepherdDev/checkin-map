﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CheckinMapKiosk.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.CheckinMap.ServeInArea" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <style type="text/css">
            .kiosk-body
            {
                position: absolute;
                left: 0px;
                top: 0px;
                width: 100%;
                height: 100%;
            }
            .kiosk-body .container {
                width: 100%;
            }
            footer {
                width: 100%;
                padding-top: 15px;
            }

            @media (min-width: 768px)
            {
                .kiosk-phone-keypad {
                    text-align: left;
                }
            }
            @media (max-width: 767px)
            {
                .btn-kiosk {
                    font-size: 22px;
                }
                header h1 {
                    font-size: 28px;
                    margin-top: 15px;
                }
            }
        </style>

        <asp:Panel ID="pnlBody" runat="server" CssClass="js-kioskscrollpanel">
            <asp:HiddenField ID="hfGroupId" runat="server" />
            <asp:HiddenField ID="hfScheduleID" runat="server" />

            <header class="container">
                <h1 id="hHeader" runat="server"></h1>
            </header>

           <main class="clearfix js-scrollcontainer">
                <div class="scrollpanel">
                    <div class="scroller">
                        <div style="text-align: center;">
                            <img id="imgImageMap" runat="server" style="max-width: 100%;" src="." />
                        </div>
                        <asp:LinkButton ID="lbSelectGroup" runat="server" OnCommand="lbSelectGroup_Command" Text="Test" CssClass="hidden" />
                    </div>
                </div>
            </main>

            <footer class="container">
                <asp:LinkButton ID="lbBack" runat="server" OnClick="lbBack_Click" CssClass="btn btn-default btn-kiosk">Back</asp:LinkButton>
            </footer>

            <asp:HiddenField ID="hfMapData" runat="server" />
            <script type="text/javascript">
                if (!window.Base64)
                {
                    window.Base64 = { _keyStr: "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=", encode: function (e) { var t = ""; var n, r, i, s, o, u, a; var f = 0; e = Base64._utf8_encode(e); while (f < e.length) { n = e.charCodeAt(f++); r = e.charCodeAt(f++); i = e.charCodeAt(f++); s = n >> 2; o = (n & 3) << 4 | r >> 4; u = (r & 15) << 2 | i >> 6; a = i & 63; if (isNaN(r)) { u = a = 64 } else if (isNaN(i)) { a = 64 } t = t + this._keyStr.charAt(s) + this._keyStr.charAt(o) + this._keyStr.charAt(u) + this._keyStr.charAt(a) } return t }, decode: function (e) { var t = ""; var n, r, i; var s, o, u, a; var f = 0; e = e.replace(/[^A-Za-z0-9+/=]/g, ""); while (f < e.length) { s = this._keyStr.indexOf(e.charAt(f++)); o = this._keyStr.indexOf(e.charAt(f++)); u = this._keyStr.indexOf(e.charAt(f++)); a = this._keyStr.indexOf(e.charAt(f++)); n = s << 2 | o >> 4; r = (o & 15) << 4 | u >> 2; i = (u & 3) << 6 | a; t = t + String.fromCharCode(n); if (u != 64) { t = t + String.fromCharCode(r) } if (a != 64) { t = t + String.fromCharCode(i) } } t = Base64._utf8_decode(t); return t }, _utf8_encode: function (e) { e = e.replace(/rn/g, "n"); var t = ""; for (var n = 0; n < e.length; n++) { var r = e.charCodeAt(n); if (r < 128) { t += String.fromCharCode(r) } else if (r > 127 && r < 2048) { t += String.fromCharCode(r >> 6 | 192); t += String.fromCharCode(r & 63 | 128) } else { t += String.fromCharCode(r >> 12 | 224); t += String.fromCharCode(r >> 6 & 63 | 128); t += String.fromCharCode(r & 63 | 128) } } return t }, _utf8_decode: function (e) { var t = ""; var n = 0; var r = c1 = c2 = 0; while (n < e.length) { r = e.charCodeAt(n); if (r < 128) { t += String.fromCharCode(r); n++ } else if (r > 191 && r < 224) { c2 = e.charCodeAt(n + 1); t += String.fromCharCode((r & 31) << 6 | c2 & 63); n += 2 } else { c2 = e.charCodeAt(n + 1); c3 = e.charCodeAt(n + 2); t += String.fromCharCode((r & 15) << 12 | (c2 & 63) << 6 | c3 & 63); n += 3 } } return t } }
                }

                (function ($) {
                    var updateTimer = null;
                    var updateAjax = null;

                    /* Update the actions asynchronously. This keeps the button status up to date. */
                    function updateActions()
                    {
                        var location = window.location.href.substr(0, window.location.href.indexOf('?'));

                        updateTimer = null;
                        updateAjax = $.getJSON(location + '?groupId=' + $('#<%= hfGroupId.ClientID %>').val() + '&scheduleId=' + $('#<%= hfScheduleID.ClientID %>').val() + '&json=1', function (data, status, xhr)
                        {
                            $('#<%= imgImageMap.ClientID %>').ImageMap('setActions', data);

                            updateTimer = setTimeout(updateActions, <%= GetAttributeValue( "RefreshTime" ) %> * 1000);
                            updateAjax = null;
                        });
                    }

                    Sys.WebForms.PageRequestManager.getInstance().add_beginRequest(function ()
                    {
                        if (updateAjax != null)
                        {
                            updateAjax.abort();
                            updateAjax = null;
                        }
                        if (updateTimer != null)
                        {
                            clearTimeout(updateTimer);
                            updateTimer = null;
                        }
                    });

                    Sys.WebForms.PageRequestManager.getInstance().add_pageLoaded(function ()
                    {
                        var actions = JSON.parse(window.Base64.decode($('#<%= hfMapData.ClientID %>').val()));
                        $('#<%= imgImageMap.ClientID %>').ImageMap({ edit: false, actions: actions });

                        updateTimer = setTimeout(updateActions, <%= GetAttributeValue( "RefreshTime" ) %> * 1000);
                    });
                })(jQuery);

                function imgImageMapLoaded()
                {
                    if (typeof bodyScroll !== 'undefined' && bodyScroll)
                    {
                        bodyScroll.refresh();
                    }
                }
            </script>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

