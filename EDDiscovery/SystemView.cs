using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EDDiscovery2
{
    public partial class SystemView : Form
    {
        private readonly AutoCompleteStringCollection _systemNames;

        public SystemView(AutoCompleteStringCollection SystemNames)
        {
            _systemNames = SystemNames;
            InitializeComponent();
        }



        private void textBox_From_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
