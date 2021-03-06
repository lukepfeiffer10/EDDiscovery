﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EDDiscovery2.DB.InMemory
{
    public class VisitedSystemsClass : IVisitedSystems
    {
        // For when you need a minimal version and don't want to mess up the database. 
        // Useful for creation of test doubles
        public int id { get; set; }
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public int Commander { get; set; }
        public int Source { get; set; }
        public string Unit { get; set; }
        public bool EDSM_sync { get; set; }
        public int MapColour { get; set; }

        public bool Update()
        {
            // Nothing to persist to
            return true;
        }
    }
}
