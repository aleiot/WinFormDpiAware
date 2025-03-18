using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace System.Windows.DpiAwareForms
{
    public enum Placement
    {
        Undefined,
        Center,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public partial class DpiAwareForm : Form
    {
        private const int Tolerance = 100;

        private readonly DpiHelper _dpiHelp;
        private readonly Placement _place;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool UpdateLocked { get; set; } = false;

        protected DpiAwareForm()
            : this(Placement.Center)
        { }

        protected DpiAwareForm(Placement place)
        {
            DoubleBuffered = true;

            _place = place;
            _dpiHelp = new DpiHelper(this);

            InitializeComponent();
        }

        /// <summary>
        /// If helped Control is a Form, places it centered in the screen where first form of
        /// the application has been opened
        /// </summary>
        /// <param name="sender">Control being loaded</param>
        /// <param name="e">Arguments (not used)</param>
        protected void OnLoad(object sender, EventArgs e)
        {
            if (!DesignMode) ScaleOnCreation();

            try
            {
                // If user has not defined a placement, the value defined in form properties is used
                if (_place == Placement.Undefined) return;

                if (!DesignMode && _dpiHelp.FirstOpenedForm != null)
                {
                    // Screen where owner Control of this form is placed
                    var ownerScreen = Screen.FromControl(_dpiHelp.FirstOpenedForm);
                    // Screen where this form needs to be placed
                    var myScreen = Screen.AllScreens.FirstOrDefault(s => s.Equals(ownerScreen)) ?? ownerScreen;
                    int leftStep = myScreen.WorkingArea.X,
                        topStep = myScreen.WorkingArea.Y;

                    switch (_place)
                    {
                        case Placement.TopLeft:
                            leftStep += Tolerance;
                            topStep += Tolerance;
                            break;
                        case Placement.TopRight:
                            leftStep += myScreen.WorkingArea.Width - Width - Tolerance;
                            topStep += Tolerance;
                            break;
                        case Placement.BottomLeft:
                            leftStep += Tolerance;
                            topStep += myScreen.WorkingArea.Height - Height - Tolerance;
                            break;
                        case Placement.BottomRight:
                            leftStep += myScreen.WorkingArea.Width - Width - Tolerance;
                            topStep += myScreen.WorkingArea.Height - Height - Tolerance;
                            break;
                        case Placement.Center:
                            leftStep += myScreen.WorkingArea.Width / 2 - Width / 2;
                            topStep += myScreen.WorkingArea.Height / 2 - Height / 2;
                            break;
                    }

                    Left = leftStep;
                    Top = topStep;
                }
            }
            catch (Exception)
            {
                Left = Width / 2;
                Top = Height / 2;
            }
        }

        public void ScaleOnResize()
        {
            if (InvokeRequired)
                Invoke(new MethodInvoker(ScaleOnResize));
            else
                _dpiHelp.ScaleOnResize();
        }

        private void ScaleOnCreation() => _dpiHelp.ScaleOnCreation();

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(factor, specified);

            _dpiHelp.ScaleControl(factor);
        }
    }
}