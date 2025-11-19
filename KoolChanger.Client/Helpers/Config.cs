using CSLOLTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoolChanger.Helpers
{
    public class Config
    {
        public Dictionary<string, Skin> SelectedSkins { get; set; } = new();
        public string GamePath { get; set; } = "";
        public string PartyModeUrl { get; set; } = "";
    }
}