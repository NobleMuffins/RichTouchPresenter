using System;
using Cirrious.MvvmCross.Touch.Views.Presenters;
using Cirrious.MvvmCross.Touch.Platform;
using UIKit;
using Cirrious.MvvmCross.Touch.Views;
using System.Collections.Generic;

namespace NobleMuffins.RichTouchPresenter
{
	public class TouchPresenter: MvxTouchViewPresenter
	{
		public TouchPresenter (MvxApplicationDelegate appDelegate, UIWindow window)
			: base(appDelegate, window)
		{
		}

		private readonly ICollection<IPresentationHost> presentationHosts = new HashSet<IPresentationHost>();
		private readonly IDictionary<object,Action> removalAgentsByViewModel = new Dictionary<object,Action>();

		public override void Show (Cirrious.MvvmCross.Touch.Views.IMvxTouchView view)
		{
			if (view is IPresentationHost) {
				presentationHosts.Add ((IPresentationHost) view);
			}

			bool presentedByHost = false;

			foreach (var host in presentationHosts) {
				var doThisOne = host.ShouldPresentViewController (view);
				if (doThisOne) {
					Action dismissalAgent = null;
					host.PresentViewController (view, out dismissalAgent);
					if (dismissalAgent == null) {
						throw new NullReferenceException ("IPresentationHost.PresentViewController must yield a dismissal agent if .ShouldPresentViewController yields true.");
					}
					removalAgentsByViewModel [view.ViewModel] = dismissalAgent;
					presentedByHost = true;
				}
				if (presentedByHost) {
					break;
				}
			}

			if (!presentedByHost) {
				base.Show (view);
			}
		}

		public override void Close (Cirrious.MvvmCross.ViewModels.IMvxViewModel toClose)
		{
			if (toClose is IPresentationHost && presentationHosts.Contains ((IPresentationHost) toClose)) {
				presentationHosts.Remove ((IPresentationHost) toClose);
			}
			if (removalAgentsByViewModel.ContainsKey (toClose)) {
				removalAgentsByViewModel [toClose] ();
				removalAgentsByViewModel.Remove (toClose);
			} else {
				base.Close (toClose);
			}
		}
	}
}

