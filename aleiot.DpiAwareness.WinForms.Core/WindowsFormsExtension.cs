using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace aleiot.DpiAwareWinForms;

public static class WindowsFormsExtension
{
    /// <summary>
    /// Checks if this Control needs to be scaled according to his parent's Font size
    /// </summary>
    /// <param name="child">Control that may need scaling</param>
    /// <param name="parent">Parent control, a.k.a. the closer ContainerControl in the chain</param>
    /// <returns>True if given Control is to scale, false otherwise</returns>
    public static bool ToScale(this Control child, Control parent)
    {
        return Math.Abs(child.Font.SizeInPoints - parent.Font.SizeInPoints) > 0.001f;
    }

    /// <summary>
    /// Scale Font property of Control, changing also his selected graphics unit to points if requested
    /// </summary>
    /// <param name="ctrl">Control that needs Font scaling</param>
    /// <param name="factor">Scale factor</param>
    /// <param name="toPoints">Indicates if Font size will be in points</param>
    public static void ScaleFont(this Control ctrl, float factor, bool toPoints)
        => ctrl.Font = ctrl.Font.Scale(factor, toPoints);

    /// <summary>
    /// Scales a Font independent from control object
    /// </summary>
    /// <param name="baseFont">Font to scale</param>
    /// <param name="factor">Scale factor</param>
    /// <param name="toPoints">Indicates if scaling operation have to convert Fonts in points or not</param>
    /// <returns>Font scaled</returns>
    public static Font Scale(this Font baseFont, float factor, bool toPoints)
        => new Font(baseFont.FontFamily, baseFont.Size * factor, baseFont.Style, toPoints ? GraphicsUnit.Point : baseFont.Unit);

    /// <summary>
    /// Checks if this ToolStripItem needs to be scaled according to his parent's Font size
    /// </summary>
    /// <param name="child">ToolStripItem that may need scaling</param>
    /// <param name="parent">Parent control, a.k.a. the closer ContainerControl in the chain</param>
    /// <returns>True if given ToolStripItem is to scale, false otherwise</returns>
    public static bool ToScale(this ToolStripItem child, ToolStrip parent)
        => Math.Abs(child.Font.SizeInPoints - parent.Font.SizeInPoints) > 0.001f;

    /// <summary>
    /// Scale Font property of ToolStripItem, changing also his selected graphics unit to points if requested
    /// </summary>
    /// <param name="item">ToolStripItem that needs Font scaling</param>
    /// <param name="factor">Scale factor</param>
    /// <param name="toPoints">Indicates if Font size will be in points</param>
    public static void ScaleFont(this ToolStripItem item, float factor, bool toPoints)
        => item.Font = item.Font.Scale(factor, toPoints);

    /// <summary>
    /// Scales ListView's columns width, that is not affected by standard .NET scaling
    /// </summary>
    /// <param name="lView">ListView that needs columns scaling</param>
    /// <param name="widthFactor">Scale factor</param>
    public static void ScaleColumns(this ListView lView, float widthFactor)
    {
        foreach (ColumnHeader column in lView.Columns)
            column.Width = (int)Math.Round(column.Width * widthFactor);
    }

    /// <summary>
    /// Checks if this ListViewItem needs to be scaled according to his parent's Font size
    /// </summary>
    /// <param name="child">ListViewItem that may need scaling</param>
    /// <param name="parent">Parent control, a.k.a. the closer ContainerControl in the chain</param>
    /// <returns>True if given ListViewItem is to scale, false otherwise</returns>
    public static bool ToScale(this ListViewItem itm, Control parent)
    {
        return Math.Abs(itm.Font.SizeInPoints - parent.Font.SizeInPoints) > 0.001f;
    }

    /// <summary>
    /// Scale Font property of ListViewItem, changing also his selected graphics unit to points if requested
    /// </summary>
    /// <param name="item">ListViewItem that needs Font scaling</param>
    /// <param name="factor">Scale factor</param>
    /// <param name="toPoints">Indicates if Font size will be in points</param>
    public static void ScaleFont(this ListViewItem item, float factor, bool toPoints)
        => item.Font = item.Font.Scale(factor, toPoints);

    /// <summary>
    /// Gets DPI of screen where the control is currently displayed
    /// </summary>
    /// <param name="ctrl">Current control</param>
    /// <returns>DPI of screen where this control is painted</returns>
    public static uint GetDpi(this Control ctrl)
    {
        try
        {
            return GetDpiForWindow(ctrl.Handle);
        }
        catch (Win32Exception)
        {
            // Exception thrown when closing application, return fake value because it is not used
            return 96;
        }
        catch (Exception)
        {
            return (uint)ctrl.CreateGraphics().DpiX;
        }
    }

    [DllImport("User32.dll")]
    private static extern uint GetDpiForWindow([In] IntPtr window);
}