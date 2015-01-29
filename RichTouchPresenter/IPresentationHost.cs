using System;
using UIKit;
using Cirrious.MvvmCross.Touch.Views;

namespace NobleMuffins.RichTouchPresenter
{
	public interface IPresentationHost
	{
		bool ShouldPresentViewController(IMvxTouchView view);
		void PresentViewController(IMvxTouchView view, out Action dismissalAgent);
	}
}

