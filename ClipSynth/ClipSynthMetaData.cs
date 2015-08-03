using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Utilities;

namespace ClipSynth
{
    public class ClipSynthMetaData : LoadSaver<ClipSynthMetaData>
    {
        public string Movie = String.Empty;
        public string Thumbnail = String.Empty;
        public int Frames = 0;

        public ClipSynthMetaData()
        {
            CreateOnFileNotExists = true;
        }
    }
}
