﻿using System.ComponentModel;
using System.IO;
using System.Xml;

namespace CoH2XML2JSON {
    
    public class SlotItem : BP {

        public override string ModGUID { get; }

        public override ulong PBGID { get; }

        public override string Name { get; }

        public UI Display { get; }

        [DefaultValue("")]
        public string WPB { get; }

        [DefaultValue(0)]
        public int SlotSize { get; }

        public SlotItem(XmlDocument xmlDocument, string guid, string name) {

            // Set the name
            this.Name = name;

            // Set mod GUID
            this.ModGUID = guid;

            // Load pbgid
            this.PBGID = ulong.Parse(xmlDocument["instance"]["uniqueid"].GetAttribute("value"));

            // Load display
            this.Display = new(xmlDocument.SelectSingleNode("//group[@name='ui_info']") as XmlElement);

            // Get slot size
            this.SlotSize = int.Parse((xmlDocument.SelectSingleNode("//float[@name='slot_size']") as XmlElement).GetAttribute("value"));

            // Get weapon
            this.WPB = Path.GetFileNameWithoutExtension((xmlDocument.SelectSingleNode("//instance_reference[@name='weapon']") as XmlElement).GetAttribute("value"));

        }

    }

}
