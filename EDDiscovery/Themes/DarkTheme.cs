using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EDDiscovery2.Themes
{
    class DarkTheme : ITheme
    {
        public string Name => "Dark";
        public Color MainBackgroundColor => ColorTranslator.FromHtml("#23272A");
        public Color SecondaryBackgroundColor => ColorTranslator.FromHtml("#2C2F33");
        public Color MainFontColor => Color.White;
        public Color SecondaryFontColor => ColorTranslator.FromHtml("#99AAB5");
        public Color AccentColor => ColorTranslator.FromHtml("#7289DA");
        public Color GridBackgroundColor => this.MainBackgroundColor;
        public Color GridForeColor => this.MainFontColor;
        public Color GridBackColor => this.MainBackgroundColor;
        public Color GridColor => this.MainBackgroundColor;
        public Color GridCellBackColor => this.SecondaryBackgroundColor;
        public Color GridCellForeColor => this.MainFontColor;
        public Color GridCellSelectionBackColor => this.AccentColor;
        public Color GridCellSelectionForeColor => this.MainFontColor;
        public bool UseVisualStyleBackColor => false;
        public ProfessionalColorTable ColorTable => new MenuColorTable();
        public ToolStripRenderer ToolStripRenderer => new MenuRenderer(this.ColorTable);
    }
}
