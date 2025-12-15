using Newtonsoft.Json;
using System.IO.Compression;
using System.Text.Json;
using KoolChanger.Core.Models;

namespace KoolChanger.Core.Services
{
    public class CustomSkinService
    {
        private ToolService _toolService;
        public List<CustomSkin> ImportedSkins { get; set; } = [];

        public CustomSkinService(ToolService toolService)
        {
            _toolService = toolService;
            GetSkins();
        }
        public void AddSkin(CustomSkin skin, string path)
        {
            _toolService.Import(path, skin.Name);
            ImportedSkins.Add(skin);
            SaveSkins();
        }
        public void RemoveSkin(CustomSkin skin)
        {
            ImportedSkins.Remove(skin);
            Directory.Delete(Path.Combine("installed", skin.Name), true);
            SaveSkins();
        }
        public void SaveSkins()
        {
            var jsonSkins = JsonConvert.SerializeObject(ImportedSkins);
            File.WriteAllText("customskins.json", jsonSkins);
        }
        public void GetSkins()
        {
            try
            {
                var skins = JsonConvert.DeserializeObject<List<CustomSkin>>(File.ReadAllText("customskins.json"));
                ImportedSkins = skins == null ? new List<CustomSkin>() : skins;
            }
            catch { }
        }
        public CustomSkin FromFile(string path)
        {
            string infoPath = "META/info.json";

            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                var entry = archive.GetEntry(infoPath);
                if (entry != null)
                {
                    using (var stream = entry.Open())
                    using (var reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        var skin = JsonConvert.DeserializeObject<CustomSkin>(json);
                        if (skin == null)
                            throw new Exception("Wrong META with custom skin: " + path);
                        return skin;
                    }
                }
                else
                {
                    throw new Exception("Wrong META with custom skin: " + path);
                }
            }
        }
    }
}
