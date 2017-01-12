<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupAttendanceRecordFromUrl.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.ServingMap.GroupAttendanceRecordFromUrl" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />

        <asp:Panel ID="pnlMessage" CssClass="panel panel-block" runat="server" Visible="false">
            <div class="panel-body">
                <asp:Panel ID="pnlContent" runat="server">
                    <asp:Literal ID="ltContent" runat="server" />
                </asp:Panel>
            </div>

            <div class="panel-footer">
                <asp:Button ID="btnDone" runat="server" CssClass="btn btn-primary" Text="Save" OnClick="btnDone_Click" />
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>