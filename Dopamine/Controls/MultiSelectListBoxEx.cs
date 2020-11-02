using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Dopamine.Controls
{
    class MultiSelectListBoxEx : ListBox
    {
        private ScrollViewer _scrollViewer;
        //private double _scrollPos = 0.0;

        public MultiSelectListBoxEx()
        {
            
        }
        public double ScrollPos
        {
            get {
                return Convert.ToDouble(GetValue(ScrollPosProperty));// GetScrollToPosition(this) ?? 0; 
            }
            set { 
                //double? newPos = SetScrollToPosition(this, value); 
                SetValue(ScrollPosProperty, value); 
            }
        }

        public static readonly DependencyProperty ScrollPosProperty =
                DependencyProperty.Register(nameof(ScrollPos), typeof(double), typeof(MultiSelectListBoxEx), new PropertyMetadata(.0));

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_scrollViewer == null)
            {
                _scrollViewer = GetScrollViewer((ListBox)this);
                _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            //double pos = Convert.ToDouble(GetValue(ScrollPosProperty));
            SetScrollToPosition(this, ScrollPos);
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            NLog.LogManager.GetLogger("ScrollViewer_ScrollChanged").Debug($"{e.VerticalChange},{e.ExtentHeightChange},{e.ExtentWidthChange}");
            if (e.VerticalChange != 0 && e.ExtentHeightChange == 0)
            {
                NLog.LogManager.GetLogger("ScrollViewer_ScrollChanged").Debug("NEW SCROLL POS: " + e.VerticalOffset.ToString());
                ScrollPos = e.VerticalOffset;
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MultiSelectListBoxItem();
        }

        private static ScrollViewer GetScrollViewer(ListBox lb)
        {
            Decorator border = VisualTreeHelper.GetChild(lb, 0) as Decorator;
            if (border != null)
                return border.Child as ScrollViewer;
            return null;
        }

        private static double? GetScrollToPosition(ListBox lb)
        {
            ScrollViewer scrollViewer = GetScrollViewer(lb);
            if (scrollViewer == null)
                return null;
            return scrollViewer.VerticalOffset;
        }

        private static double? SetScrollToPosition(ListBox lb, double position)
        {
            ScrollViewer scrollViewer = GetScrollViewer(lb);
            if (scrollViewer == null)
                return null;
            scrollViewer.ScrollToVerticalOffset(position);
            scrollViewer.UpdateLayout();
            return scrollViewer.VerticalOffset;
        }

        public class MultiSelectListBoxItem : ListBoxItem
        {
            private bool deferSelection = false;

            protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
            {

                if ((e.ClickCount == 1 && IsSelected))
                {
                    // The user may start a drag by clicking on the selected items
                    // Delay destroying the selection to the Up event
                    this.deferSelection = true;
                }
                else
                {
                    base.OnMouseLeftButtonDown(e);
                }
            }

            protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
            {
                if (this.deferSelection)
                {
                    try
                    {
                        base.OnMouseLeftButtonDown(e);
                    }
                    finally
                    {
                        this.deferSelection = false;
                    }

                    base.OnMouseLeftButtonUp(e);
                }
            }

            protected override void OnMouseLeave(MouseEventArgs e)
            {
                // Abort deferred Down
                this.deferSelection = false;
                base.OnMouseLeave(e);
            }

        }
    }
}
