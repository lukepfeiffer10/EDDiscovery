using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EDDiscovery2.Themes
{
    public interface ITheme
    {
        string Name { get; }
        Color MainBackgroundColor { get;}
        Color SecondaryBackgroundColor { get; }
        Color MainFontColor { get; }
        Color SecondaryFontColor { get; }
        Color AccentColor { get; }
        Color GridBackgroundColor { get; }
        Color GridForeColor { get; }
        Color GridBackColor { get; }
        Color GridColor { get; }
        Color GridCellBackColor { get; }
        Color GridCellForeColor { get; }
        Color GridCellSelectionBackColor { get; }
        Color GridCellSelectionForeColor { get; }
        bool UseVisualStyleBackColor { get; }
        ProfessionalColorTable ColorTable { get; }
        ToolStripRenderer ToolStripRenderer { get; }
    }
}
