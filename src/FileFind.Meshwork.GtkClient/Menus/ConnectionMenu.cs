//
// ConnectionMenu.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using Gtk;
using Glade;
using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using FileFind.Meshwork.Transport;

namespace FileFind.Meshwork.GtkClient
{
	public class ConnectionMenu
	{
		[Widget] MenuItem mnuConnectionsConnect;
		[Widget] MenuItem mnuConnectionsDisconnect;
		[Widget] MenuItem mnuConnectionsDelete;
		Menu mnuConnections;
		ITransport selectedConnection;
		TreeView connectionList;

		public ConnectionMenu (TreeView connectionList)
		{
			this.connectionList = connectionList;
			Glade.XML xmlMnuConnections = new Glade.XML(null, "FileFind.Meshwork.GtkClient.meshwork.glade", "mnuConnections", null);
			mnuConnections = (xmlMnuConnections.GetWidget("mnuConnections") as Gtk.Menu);
			xmlMnuConnections.Autoconnect (this);
		}

		public void Popup (ITransport connection)
		{
			selectedConnection = connection;
			mnuConnections.Popup ();
		}

		public void on_mnuConnections_show(object o, EventArgs e)
		{
			if (selectedConnection != null) {
				if (selectedConnection.State == TransportState.Disconnected & (selectedConnection.ConnectionType == ConnectionType.NodeConnection)) {
					mnuConnectionsDisconnect.Visible = false;
					mnuConnectionsConnect.Visible = true;
					mnuConnectionsDelete.Sensitive = true;
				}
				else {
					mnuConnectionsDisconnect.Visible = true;
					mnuConnectionsConnect.Visible = false;

					mnuConnectionsDisconnect.Sensitive = true;
					mnuConnectionsDelete.Sensitive = true;
				}
			} else {
				mnuConnectionsDisconnect.Visible = true;
				mnuConnectionsConnect.Visible = false;

				mnuConnectionsDisconnect.Sensitive = false;
				mnuConnectionsDelete.Sensitive = false;
			}
		}

		public void on_mnuConnectionsConnect_activate(object o, EventArgs e)
		{
			// XXX: This will need to change when we start support things other
			// than just TCP sockets.
			IPEndPoint endpoint = (IPEndPoint)selectedConnection.RemoteEndPoint;
			selectedConnection.Network.ConnectTo(new TcpTransport(endpoint.Address, endpoint.Port, ConnectionType.NodeConnection));
		}
				
		public void on_mnuConnectionsDisconnect_activate(object o, EventArgs e)
		{
			selectedConnection.Disconnect();
		}
		
		public void on_mnuConnectionsDelete_activate (object o, EventArgs e)
		{
			Core.TransportManager.Remove(selectedConnection);
		}
		
		public void on_mnuConnectionsClearDisconnected_activate(object o, EventArgs e)
		{
			List<ITransport> toRemove = new List<ITransport>();
			
			foreach (object[] row in (ListStore)connectionList.Model) {
				ITransport transport = (ITransport)row[0];
				if (transport.State == TransportState.Disconnected) {
					toRemove.Add(transport);
				} 
			}
			
			foreach (ITransport transport in toRemove) {
				Core.TransportManager.Remove(transport);
			}
		}

	}
}
