using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EDDiscovery2.Themes
{
    class LightTheme :ITheme
    {
        public string Name => "Light";
        public Color MainBackgroundColor => default(Color);
        public Color SecondaryBackgroundColor => default(Color);
        public Color MainFontColor => default(Color);
        public Color SecondaryFontColor => default(Color);
        public Color AccentColor => default(Color);
        public Color GridBackgroundColor => SystemColors.AppWorkspace;
        public Color GridForeColor => SystemColors.ControlText;
        public Color GridBackColor => SystemColors.AppWorkspace;
        public Color GridColor => SystemColors.ControlDark;
        public Color GridCellBackColor => SystemColors.Window;
        public Color GridCellForeColor => SystemColors.ControlText;
        public Color GridCellSelectionBackColor => SystemColors.Highlight;
        public Color GridCellSelectionForeColor => SystemColors.HighlightText;
        public bool UseVisualStyleBackColor => true;
        public ProfessionalColorTable ColorTable => new ProfessionalColorTable();
        public ToolStripRenderer ToolStripRenderer => new MenuRenderer();
    }
}
