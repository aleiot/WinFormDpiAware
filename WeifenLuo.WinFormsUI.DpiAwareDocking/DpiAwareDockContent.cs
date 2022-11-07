using System;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace WeifenLuo.WinFormsUI.DpiAwareDocking
{
    public partial class DpiAwareDockContent : DockContent
    {
        private readonly DockingDpiHelper _dpiHelp;

        public DpiAwareDockContent()
        {
            _dpiHelp = new DockingDpiHelper(this);

            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            if (!DesignMode) ScaleOnCreation();
        }

        public void ScaleOnCreation() => _dpiHelp.ScaleOnCreation();

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(factor, specified);

            _dpiHelp.ScaleControl(factor);
        }
    }
}
