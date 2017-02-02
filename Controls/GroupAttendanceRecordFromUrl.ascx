<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupAttendanceRecordFromUrl.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.ServingMap.GroupAttendanceRecordFromUrl" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />

        <Rock:NotificationBox ID="nbSuccess" runat="server" NotificationBoxType="Success" />
    </ContentTemplate>
</asp:UpdatePanel>