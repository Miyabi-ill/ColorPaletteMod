using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Terraria;

namespace ColorPalette
{
    class PaletteIO
    {
        public string name;

        public List<ColorData> datas = new List<ColorData>();

        [JsonIgnore]
        public bool requireSave = false;

        [JsonIgnore]
        public static List<PaletteIO> palettes = new List<PaletteIO>();

        public static void ReadAllPalette()
        {
            string directory = Path.Combine(Main.SavePath, "Palettes");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var paths = Directory.GetFiles(directory);
            foreach (var path in paths)
            {
                using (var stream = new StreamReader(path))
                {
                    var palette = JsonConvert.DeserializeObject<PaletteIO>(stream.ReadToEnd());
                    palettes.Add(palette);
                }
            }
        }

        public static void SaveAllPalette()
        {
            string directory = Path.Combine(Main.SavePath, "Palettes");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            foreach (var palette in palettes)
            {
                using (var stream = new StreamWriter(Path.Combine(directory, palette.name + ".json")))
                {
                    var text = JsonConvert.SerializeObject(palette, Formatting.Indented);
                    stream.Write(text);
                }
            }
        }
    }

    class ColorData
    {
        public int id;

        public string fullName = "";

        public byte paint;
    }
}
