using System;
using FileFind.Meshwork.GtkClient.Pages;
using Gtk;

namespace FileFind.Meshwork.GtkClient.Menus
{	
	public class ChatPageMenu
	{
		ChatSubpageBase m_Page;
		Menu m_Menu;
		
		public ChatPageMenu(ChatSubpageBase page)
		{
			m_Page = page;
			m_Menu = new Menu();
			var closeItem = new ImageMenuItem(Stock.Close, null);
			closeItem.Activated += HandleCloseItemActivated;
			m_Menu.Append(closeItem);
			m_Menu.ShowAll();
		}
		
		public void Popup ()
		{
			m_Menu.Popup();
		}

		void HandleCloseItemActivated(object sender, EventArgs e)
		{
			m_Page.Close();
		}
	}
}
