using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace aleiot.DpiAwareWinForms;

/// <summary>
/// Structure that provides division operator for a SizeF structure
/// </summary>
public struct DivisibleSizeF
{
    /// <summary>
    /// Underlying SizeF structure
    /// </summary>
    private SizeF _size;

    public readonly float Width => _size.Width;
    public readonly float Height => _size.Height;

    public static DivisibleSizeF operator /(DivisibleSizeF a, DivisibleSizeF b)
    {
        return new DivisibleSizeF(a.Width / b.Width, a.Height / b.Height);
    }

    public static explicit operator SizeF(DivisibleSizeF a)
    {
        return a._size;
    }

    public DivisibleSizeF(float width, float height)
    {
        _size = new SizeF(width, height);
    }
}

/// <summary>
/// Class providing basic operations to properly scale a Control object depending on current monitor DPI value
/// </summary>
public class DpiHelper
{
    [DllImport("user32.dll")]
    static extern bool LockWindowUpdate(IntPtr hWndLock);

    #region Constants

    /// <summary>
    /// Standard DPI value for application design, equals to a 100% scale factor
    /// </summary>
    public const uint StandardDesignDpi = 96;
    /// <summary>
    /// Standard conversion factor from pixel to points, using StandardDesignDpi value
    /// </summary>
    public const float PixelToPointsFactor = 72f / StandardDesignDpi;
    /// <summary>
    /// Standard conversion factor from points to pixel, using StandardDesignDpi value
    /// </summary>
    public const float PointsToPixelFactor = StandardDesignDpi / 72f;

    #endregion

    #region Attributes and Properties

    #region Attributes

    /// <summary>
    /// DPI setting of last monitor where helped Control requested a scaling
    /// </summary>
    private uint _lastMonitorDpi;
    /// <summary>
    /// True when scaling control, false otherwise
    /// </summary>
    private bool _scaling;

    private bool _toolStripToScale = true;

    /// <summary>
    /// Control object that require DPI scaling features
    /// </summary>
    protected readonly Control HelpedControl;
    protected bool ScaledOnCreation = false;

    /// <summary>
    /// Dictionary linking Control type with specific scale actions to be done in ScaleChilds method
    /// </summary>
    protected Dictionary<Type, Action<Control, float, bool>> ScaleActions = new Dictionary<Type, Action<Control, float, bool>>();

    #endregion

    #region Properties

    /// <summary>
    /// Gets X coordinate scale factor of primary screen
    /// </summary>
    public static float BaseScaleFactorX => Graphics.FromHwnd(IntPtr.Zero).DpiX / StandardDesignDpi;
    /// <summary>
    /// Gets Y coordinate scale factor of primary screen
    /// </summary>
    public static float BaseScaleFactorY => Graphics.FromHwnd(IntPtr.Zero).DpiY / StandardDesignDpi;
    /// <summary>
    /// It is the first form opened by the application, used for initial placement of the helped Control
    /// </summary>
    public Form FirstOpenedForm { get; }

    #endregion

    #endregion

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="ctrl">Control object that requires DPI scaling features</param>
    public DpiHelper(Control ctrl)
    {
        bool designMode = LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        HelpedControl = ctrl;

        // Get the first opened form, it will be used as refernce for scaling
        // Avoid using the SplashScreen, if any, because it is typically launched on a different thread and it would cause a crash
        FirstOpenedForm = Application.OpenForms.Cast<Form>()
            .FirstOrDefault(form => !form.Name.ToLower().Contains("splashscreen"));

        if (!designMode && ctrl is Form frm)
            frm.FormClosing += OnFormClosing;

        ScaleActions.Add(typeof(ToolStrip), (parent, factor, toPoints) => ScaleChildrenToolStripItems((ToolStrip)parent, factor, toPoints));
        ScaleActions.Add(typeof(ListView), (parent, factor, toPoints) => ScaleChildrenListViewItems((ListView)parent, factor, toPoints));
    }

    /// <summary>
    /// Calculates DPI scale factor between current monitor and previous monitor
    /// </summary>
    /// <returns>DPI scale factor of current monitor</returns>
    public float GetScreenScaleFactor()
    {
        var currentMonitorDpi = HelpedControl.GetDpi();
        var newScaleFactor = (float)currentMonitorDpi / _lastMonitorDpi;

        if (ToScale(newScaleFactor))
            _lastMonitorDpi = currentMonitorDpi;

        return newScaleFactor;
    }

    /// <summary>
    /// Checks if helped Control needs to be scaled on creation (right after InitializeComponent() method call)
    /// </summary>
    protected virtual bool ToScaleOnCreation()
    {
        return !ScaledOnCreation;
    }

    /// <summary>
    /// If helped Control is a Form, OnResize event handler is detached when closing it to avoid
    /// flickering and unnecessary successive resizing
    /// </summary>
    /// <param name="sender">Control being closed</param>
    /// <param name="e">Arguments (not used)</param>
    private void OnFormClosing(object sender, FormClosingEventArgs e)
    {
        HelpedControl.Resize -= OnResize;
    }

    /// <summary>
    /// Resize event handler for helped control, called also when it is moved to a different monitor
    /// </summary>
    /// <param name="sender">Control being resized</param>
    /// <param name="e">Arguments (not used)</param>
    private void OnResize(object sender, EventArgs e)
    {
        //LockControlUpdate();

        if (!_scaling)
        {
            _scaling = true;
            ScaleOnResize();
            _scaling = false;
        }

        //LockControlUpdate();
    }

    /// <summary>
    /// Checks it current scale factor justifies the loop on form controls to scale them all
    /// </summary>
    /// <param name="factor">Current scale factor</param>
    /// <returns>True if scaling is needed, false otherwise</returns>
    private bool ToScale(float factor)
    {
        return Math.Abs(factor - 1) > 0.001f;
    }

    /// <summary>
    /// Scales helped control on creation
    /// </summary>
    /// <remarks>To be called right AFTER constructor's InitializeComponent() method call</remarks>
    public virtual void ScaleOnCreation()
    {
        InternalScaleOnCreation(false);
    }

    protected void InternalScaleOnCreation(bool helpingDockContent)
    {
        if (ToScaleOnCreation())
        {
            Scale(PixelToPointsFactor, true);

            if (!helpingDockContent)
            {
                _lastMonitorDpi = HelpedControl.GetDpi();
                HelpedControl.Resize += OnResize;
            }

            ScaledOnCreation = true;
        }
    }

    /// <summary>
    /// Performs scaling on helped control, changing its font size
    /// </summary>
    public void ScaleOnResize()
    {
        Scale(GetScreenScaleFactor(), false);
    }

    protected void LockControlUpdate()
    {
        // Checks if _helpedControl is a DpiAwareForm, if yes it has the needed property UpdateLocked
        if (HelpedControl is DpiAwareForm f)
            LockWindowUpdate(f.UpdateLocked ? IntPtr.Zero : f.Handle);
    }

    /// <summary>
    /// Scales helped control and all its childs
    /// </summary>
    /// <param name="scale">Scaling factor</param>
    /// <param name="toPoints">Indicates if font must be converted to points</param>
    private void Scale(float scale, bool toPoints)
    {
        if (ToScale(scale))
        {
            HelpedControl.ScaleFont(scale, toPoints);
            ScaleChildren(HelpedControl, scale, toPoints);
        }
    }

    /// <summary>
    /// Recursively scales child controls with Font different from their container Font (this means all classes extending UserControl)
    /// </summary>
    /// <param name="parent">Parent control, already scaled</param>
    /// <param name="factor">Scale factor applied to child control's Fonts</param>
    /// <param name="toPoints">Indicates if scaling operation have to convert Fonts in points or not</param>
    protected void ScaleChildren(Control parent, float factor, bool toPoints)
    {
        foreach (Control ctrl in parent.Controls)
        {
            if (ctrl.ToScale(parent))
                ctrl.ScaleFont(factor, toPoints);

            var ctrlType = ctrl.GetType();

            if (ScaleActions.ContainsKey(ctrlType))
                ScaleActions[ctrlType](ctrl, factor, toPoints);

            ScaleChildren(ctrl, factor, toPoints);
        }
    }

    private void ScaleToolStrip(ToolStrip tStrip)
    {
        tStrip.AutoSize = false;
        tStrip.ImageScalingSize = new Size((int)(tStrip.ImageScalingSize.Width * BaseScaleFactorX), (int)(tStrip.ImageScalingSize.Height * BaseScaleFactorY));
        tStrip.AutoSize = true;
    }

    private void ScaleChildrenToolStripItems(ToolStrip parent, float factor, bool toPoints)
    {
        if (_toolStripToScale)
        {
            ScaleToolStrip(parent);

            _toolStripToScale = false;
        }

        foreach (ToolStripItem item in parent.Items)
            if (item.ToScale(parent))
                item.ScaleFont(factor, toPoints);
    }

    private void ScaleChildrenListViewItems(ListView parent, float factor, bool toPoints)
    {
        foreach (ListViewItem item in parent.Items)
            if (item.ToScale(HelpedControl))
                item.ScaleFont(factor, toPoints);
    }

    public void ScaleControl(SizeF factor) => ScaleListViewColumns(HelpedControl, factor.Width);

    private void ScaleListViewColumns(Control parent, float widthFactor)
    {
        foreach (Control ctrl in parent.Controls)
            if (ctrl is ListView lView)
                lView.ScaleColumns(widthFactor);
            else
                ScaleListViewColumns(ctrl, widthFactor);
    }

    public static Font FontToPixels(Font baseFont)
        => new Font(baseFont.FontFamily, baseFont.Size * PointsToPixelFactor, baseFont.Style, GraphicsUnit.Pixel);
}