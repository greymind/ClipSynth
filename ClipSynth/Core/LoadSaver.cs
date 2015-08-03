using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Diagnostics;

namespace Core.Utilities
{
    public class LoadSaver<T> where T : LoadSaver<T>
    {
        protected Action OnLoadStart;
        protected Action OnLoadEnd;
        protected Action OnSaveStart;
        protected Action OnSaveEnd;

        [XmlIgnore]
        public String Path;

        [XmlIgnore]
        public Boolean CreateOnFileNotExists;

        public LoadSaver()
        {
            Path = String.Empty;
            CreateOnFileNotExists = false;

            OnLoadStart = null;
            OnLoadEnd = null;
            OnSaveStart = null;
            OnSaveEnd = null;
        }

        public static void Save(ref T t)
        {
            if (t != null)
            {
                if (t.OnSaveStart != null)
                {
                    t.OnSaveStart();
                }

                var serializer = new XmlSerializer(typeof(T));
                var writer = new StreamWriter(t.Path);
                serializer.Serialize(writer, t);
                writer.Close();

                if (t.OnSaveEnd != null)
                {
                    t.OnSaveEnd();
                }
            }
        }

        public static void Load(ref T t)
        {
            if (t != null)
            {
                String tPath = t.Path;
                if (File.Exists(tPath))
                {
                    if (t.OnLoadStart != null)
                    {
                        t.OnLoadStart();
                    }

                    StreamReader reader = null;
                    try
                    {
                        var deserializer = new XmlSerializer(typeof(T));
                        reader = new StreamReader(tPath);
                        t = (T)deserializer.Deserialize(reader);
                        t.Path = tPath;

                        if (t.OnLoadEnd != null)
                        {
                            t.OnLoadEnd();
                        }
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        throw ex;
#else
                        File.WriteAllText(DateTime.Now.Ticks.ToString(), ex.Message);
#endif
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Close();
                        }
                    }
                }
                else if (t.CreateOnFileNotExists)
                {
                    Save(ref t);
                }
            }
        }
    }
}
