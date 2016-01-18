using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EDDiscovery;

namespace EDDiscovery2
{
    class MenuColorTable : ProfessionalColorTable
    {
        private readonly Color black = ColorTranslator.FromHtml("#23272A");
        private readonly Color dark = ColorTranslator.FromHtml("#2C2F33");

        public override Color MenuItemSelected => dark;
        public override Color ToolStripDropDownBackground => black;
        public override Color ToolStripBorder => dark;
        public override Color MenuItemBorder => dark;
        public override Color MenuItemSelectedGradientBegin => dark;
        public override Color MenuItemSelectedGradientEnd => dark;
        public override Color MenuItemPressedGradientBegin => dark;
        public override Color MenuItemPressedGradientEnd => dark;
        public override Color MenuBorder => dark;
        public override Color ImageMarginGradientBegin => black;
        public override Color ImageMarginGradientMiddle => black;
        public override Color ImageMarginGradientEnd => black;
    }

    class MenuRenderer : ToolStripProfessionalRenderer
    {
        public MenuRenderer(): base() { }
        public MenuRenderer(ProfessionalColorTable colorTable) : base(colorTable) { }

        protected override void Initialize(ToolStrip toolStrip)
        {
            base.Initialize(toolStrip);
            toolStrip.ForeColor = EDDiscoveryForm.EDDConfig.SelectedTheme.MainFontColor;
            toolStrip.BackColor = EDDiscoveryForm.EDDConfig.SelectedTheme.MainBackgroundColor;
        }

        protected override void InitializeItem(ToolStripItem item)
        {
            base.InitializeItem(item);
            item.ForeColor = EDDiscoveryForm.EDDConfig.SelectedTheme.MainFontColor;
            item.BackColor = EDDiscoveryForm.EDDConfig.SelectedTheme.MainBackgroundColor;
            var menuItem = item as ToolStripMenuItem;
            if (menuItem != null)
            {
                foreach (ToolStripDropDownItem dropDownItem in menuItem.DropDownItems)
                {
                    dropDownItem.ForeColor = EDDiscoveryForm.EDDConfig.SelectedTheme.MainFontColor;
                }
            }
        }
    }
}
