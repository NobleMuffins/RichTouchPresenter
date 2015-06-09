using System;
using System.Linq;
using Cirrious.MvvmCross.Touch.Views.Presenters;
using Cirrious.MvvmCross.Touch.Platform;
using UIKit;
using Cirrious.MvvmCross.Touch.Views;
using System.Collections.Generic;
using Cirrious.MvvmCross.ViewModels;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.Views;

namespace NobleMuffins.RichTouchPresenter
{
	public class RichTouchPresenter: MvxTouchViewPresenter
	{
		public RichTouchPresenter (MvxApplicationDelegate appDelegate, UIWindow window)
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
			} else if (MasterNavigationController != null) {
				var topView = MasterNavigationController.TopViewController as IMvxView;
				IMvxViewModel topViewModel;

				if (topView != null) {
					topViewModel = topView.ReflectionGetViewModel ();
				} else {
//					#if DEBUG
//					MvxTrace.Warning ("Don't know how to close this ViewModel; topmost is not a touchview");
//					#endif
					return;
				}

				if (topViewModel == toClose) {
					if (MasterNavigationController.ViewControllers.Length > 1) {
						MasterNavigationController.PopViewController (true);
					} else {
						MasterNavigationController.WillMoveToParentViewController (null);
						MasterNavigationController.RemoveFromParentViewController ();
						MasterNavigationController.View.RemoveFromSuperview ();
						MasterNavigationController = null;
					}
				} else {
//					#if DEBUG
//					MvxTrace.Warning ("Don't know how to close this ViewModel; topmost view does not present this viewmodel");
//					#endif
					return;
				}
					} else {
//					#if DEBUG
//					MvxTrace.Warning("Don't know how to close this ViewModel; there are no views governed by the RichTouchPresenter or its MasterNavigationController");
//					#endif
			}
		}

	}
}

