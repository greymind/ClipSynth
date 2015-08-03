using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Core.Utilities;
using System.IO;
using IOPath = System.IO.Path;
using System.Reflection;

namespace ClipSynth
{
    public class ClipSynthProject : LoadSaver<ClipSynthProject>
    {
        public string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public UInt32 UniqueId;
        public List<ClipSynthMovie> Movies = new List<ClipSynthMovie>();
        public ClipSynthSoundtrack Soundtrack = new ClipSynthSoundtrack();

        [XmlIgnore]
        public bool Dirty = false;

        public ClipSynthProject()
        {
            Path = @"Project.clipsynth";
            CreateOnFileNotExists = true;
        }

        public UInt32 GetUniqueId()
        {
            return ++UniqueId;
        }

        public static bool TryLoad(string path, ref ClipSynthProject project)
        {
            var value = true;

            project = new ClipSynthProject();
            project.Path = path;

            try
            {
                Load(ref project);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "ClipSynth project load error");
                value = false;
            }

            return value;
        }
    }
}
