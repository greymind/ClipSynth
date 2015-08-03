using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Core.Utilities;

namespace ClipSynth
{
    public class ClipSynthSettings : LoadSaver<ClipSynthSettings>
    {
        /// <summary>
        /// The last folder opened in images mode
        /// </summary>
        public string Folder = String.Empty;

        /// <summary>
        /// A list of favorite folders opened in images mode
        /// </summary>
        public List<string> FavoriteFolders = new List<string>();

        /// <summary>
        /// The last project's path opened in movies mode
        /// </summary>
        public string Project = String.Empty;

        /// <summary>
        /// A list of favorite project's paths opened in movies mode
        /// </summary>
        public List<string> FavoriteProjects = new List<string>();

        /// <summary>
        /// The mode of operation
        /// </summary>
        public ClipSynthMode Mode = ClipSynthMode.Images;

        public ClipSynthSettings()
        {
            Path = @"ClipSynthSettings.xml";
            CreateOnFileNotExists = true;
        }
    }
}
