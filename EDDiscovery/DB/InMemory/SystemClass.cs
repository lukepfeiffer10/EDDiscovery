﻿using System;

namespace EDDiscovery2.DB.InMemory
{
    // For when you need a minimal version and don't want to mess up the database. 
    // Useful for creation of test doubles
    public class SystemClass : ISystem
    {
        public int id { get; set; }
        public string name { get; set; }
        public string SearchName { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public int cr { get; set; }
        public string CommanderCreate { get; set; }
        public DateTime CreateDate { get; set; }
        public string CommanderUpdate { get; set; }
        public DateTime UpdateDate { get; set; }
        public EDDiscovery.DB.SystemStatusEnum status { get; set; }
        public string Note { get; set; }

        public int id_eddb { get; set; }
        public string faction { get; set; }
        public long population { get; set; }
        public EDGovernment government { get; set; }
        public EDAllegiance allegiance { get; set; }
        public EDState state { get; set; }
        public EDSecurity security { get; set; }
        public EDEconomy primary_economy { get; set; }
        public int needs_permit { get; set; }
        public int eddb_updated_at { get; set; }

        public bool HasCoordinate
        {
            get
            {
                return (!double.IsNaN(x));
            }
        }
    }
}
