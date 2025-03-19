using aleiot.DpiAwareWinForms;
using System.Linq;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace WeifenLuo.WinFormsUI.DpiAwareDocking
{
    public class DockingDpiHelper : DpiHelper
    {
        public DockingDpiHelper(Control ctrl)
            : base(ctrl)
        {
            ScaleActions.Add(typeof(DockPanel), (parent, factor, toPoints) => ScaleChildrenDockContents((DockPanel)parent, factor, toPoints));
        }

        /// <summary>
        /// Checks if helped Control needs to be scaled on creation (right after InitializeComponent() method call)
        /// </summary>
        protected override bool ToScaleOnCreation()
            => !ScaledOnCreation && (!(HelpedControl is DpiAwareDockContent dc) || FirstOpenedForm.GetDpi() != 96);

        public override void ScaleOnCreation() => InternalScaleOnCreation(true);

        /// <summary>
        /// Scales all given dock pane's child dock contents, because they extend UserControl class
        /// </summary>
        /// <param name="parent">Parent dock pane, already scaled</param>
        /// <param name="factor">Scale factor applied to child dock content's Fonts</param>
        /// <param name="toPoints">Indicates if scaling operation have to convert Fonts in points or not</param>
        private void ScaleChildrenDockContents(DockPanel parent, float factor, bool toPoints)
        {
            parent.Theme.Skin.DockPaneStripSkin.TextFont.Scale(factor, toPoints);
            parent.Theme.Skin.AutoHideStripSkin.TextFont.Scale(factor, toPoints);

            foreach (var dockCnt in parent.Contents.OfType<DockContent>())
            {
                if (dockCnt.ToScale(parent))
                    dockCnt.ScaleFont(factor, toPoints);

                ScaleChildren(dockCnt, factor, toPoints);
            }
        }
    }
}
