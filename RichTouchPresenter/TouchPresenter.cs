using System;
using System.Linq;
using Cirrious.MvvmCross.Touch.Views.Presenters;
using Cirrious.MvvmCross.Touch.Platform;
using UIKit;
using Cirrious.MvvmCross.Touch.Views;
using System.Collections.Generic;
using Cirrious.MvvmCross.ViewModels;

namespace NobleMuffins.RichTouchPresenter
{
	public class TouchPresenter: MvxTouchViewPresenter
	{
		public TouchPresenter (MvxApplicationDelegate appDelegate, UIWindow window)
			: base(appDelegate, window)
		{
		}

		private readonly ICollection<IPresentationHost> presentationHosts = new HashSet<IPresentationHost>();
		private readonly IDictionary<IMvxTouchView,Action> removalAgentsByView = new Dictionary<IMvxTouchView,Action>();

		public override void Show (IMvxTouchView view)
		{
			if (view is IPresentationHost) {
				presentationHosts.Add ((IPresentationHost) view);
			}

			var potentialHosts = from host in presentationHosts
					where host.ShouldPresentViewController (view)
				select host;

			if (potentialHosts.Count () > 0) {
				var host = potentialHosts.First ();
				Action dismissalAgent = null;
				host.PresentViewController (view, out dismissalAgent);
				if (dismissalAgent == null) {
					throw new NullReferenceException ("IPresentationHost.PresentViewController must yield a dismissal agent if .ShouldPresentViewController yields true.");
				}
				removalAgentsByView [view] = dismissalAgent;
			} else {
				base.Show (view);
			}
		}

		public override void Close (IMvxViewModel toClose)
		{
			//We make an array of this to make a snapshot before modifying presentation hosts.
			var presentationHostsToDrop = (from host in presentationHosts
					where ((IMvxTouchView)host).ViewModel == toClose
				select host).ToArray();
			foreach (var host in presentationHostsToDrop) {
				presentationHosts.Remove (host);
			}
			var viewsPresentedByHosts = removalAgentsByView.Keys;
			var relevantViewsPresentedByHosts = (from view in viewsPresentedByHosts
				where view.ViewModel == toClose
				select view).ToArray();
			if (relevantViewsPresentedByHosts.Count () > 0) {
				foreach (var view in relevantViewsPresentedByHosts) {
					var removalAgent = removalAgentsByView [view];
					removalAgent ();
					removalAgentsByView.Remove (view);
				}
			} else {
				base.Close (toClose);
			}
		}
	}
}

