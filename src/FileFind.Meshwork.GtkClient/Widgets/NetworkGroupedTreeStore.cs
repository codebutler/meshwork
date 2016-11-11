//
// NetworkGroupedTreeStore.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using Gtk;

namespace FileFind.Meshwork.GtkClient.Widgets
{
	public class NetworkGroupedTreeStore<T> : TreeStore
	{
		TreeView tree;

		public NetworkGroupedTreeStore (TreeView tree) : base (typeof (Network), typeof (T))
		{
			this.tree = tree;
		}

		public TreeIter AddItem (Network network, T item)
		{
			TreeIter iter = IterForNetwork (network);
			if (iter.Equals (TreeIter.Zero)) {
				iter = this.AppendValues (network);
			}

			iter = this.AppendValues (iter, item);
			tree.ExpandToPath(base.GetPath(iter));
			return iter;
		}

		public void RemoveItem (Network network, T item)
		{
			TreeIter networkIter;
			TreeIter itemIter;
			if (this.GetIterFirst (out networkIter)) {
				do {
					if (this.IterChildren (out itemIter, networkIter)) {
						do {
							T currentItem = (T) this.GetValue (itemIter, 0);
							if (currentItem.Equals (item)) {
								this.Remove (ref itemIter);

								if (this.IterNChildren (networkIter) == 0) {
									this.Remove (ref networkIter);
								}

								break;
							}
						} while (this.IterNext (ref itemIter));
					}
				} while (this.IterNext (ref networkIter));
			}
		}
		
		public bool ContainsItem (Network network, T item)
		{
			TreeIter networkIter = IterForNetwork (network);
			if (base.IterIsValid(networkIter)) {
				TreeIter itemIter = IterForItem (networkIter, item);
				return (itemIter.Equals (TreeIter.Zero) == false);
			} else {
				return false;
			}
		}

		public TreeIter IterForNetwork (Network network)
		{
			TreeIter iter;
			if (this.GetIterFirst (out iter)) {
				do {
					Network thisNetwork = (Network)this.GetValue (iter, 0);
					if (thisNetwork == network) {
						return iter;
					}
				} while (this.IterNext (ref iter));
			} 
			return TreeIter.Zero;
		}

		private TreeIter IterForItem (TreeIter networkIter, T item)
		{
			TreeIter itemIter;
			if (this.IterChildren (out itemIter, networkIter)) {
				do {
					T currentItem = (T) this.GetValue (itemIter, 0);
					if (currentItem.Equals (item)) {
						return itemIter;
					}
				} while (this.IterNext (ref itemIter));
			}
			return TreeIter.Zero;
		}
	}
}
