#region

using System.Text.Json.Serialization.Metadata;
using KoolChanger.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace KoolChanger.Services;

public class KoolService
{
    public void Super()
    {
        // Task.Run(() =>
        // {
        //     Console.WriteLine("entered");
        //     var a = new ChampionService();
        //     var q = new SkinService();
        //     var b = a.GetChampionsAsync().GetAwaiter().GetResult();
        //     q.GetAllSkinsAsync(b).GetAwaiter().GetResult();
        //     // b.ForEach(x =>
        //     // {
        //     //     Console.WriteLine(b[0].Skins[1].Id);
        //     //     
        //     // });
        //     var extractor = new WadExtractor();
        //     var championExtract = b[0].Name;
        //     extractor.Extract(
        //         Path.Combine(RiotPathDetector.GetLeaguePath(), "DATA", "FINAL", "Champions", championExtract + ".wad.client"),
        //         Path.Combine(Path.GetTempPath(), "KoolChanger.tmp", b[0].Name + ".extracted"),
        //         Path.Combine(AppContext.BaseDirectory, "hashes.game.txt")
        //     );
        //     var rt = new RitoBin();
        //     //
        //     var sourcePath = Path.Combine(Path.GetTempPath(), "KoolChanger.tmp", championExtract + ".extracted", "data", "characters", championExtract, "skins", "skin40.bin");
        //     var destinationPath = Path.Combine(Path.GetTempPath(), "KoolChanger.tmp", championExtract + ".extracted",
        //         "data", "characters", championExtract, "skins", "skin40.bin.json");
        //     rt.ConvertBintoJson(sourcePath,
        //        destinationPath,
        //         Path.Combine(Path.GetTempPath(), "KoolChanger.tmp", championExtract + ".extracted", "data",
        //             "characters",
        //             championExtract, "skins"),
        //         Path.Combine(AppContext.BaseDirectory, "hashes")
        //     );
        //     var ParsedJson = JObject.Parse(File.ReadAllText(Path.Combine(Path.GetTempPath(), "KoolChanger.tmp",
        //         championExtract + ".extracted", "data",
        //         "characters",
        //         championExtract, "skins", "skin40.json")));
        //     var ab = ParsedJson["entries"]["value"]["items"][0];
        //     ab["key"] = $"Characters/{championExtract}/Skins/Skin0";
        //     var n = ParsedJson["entries"]["value"]["items"][0]["value"]["items"].Where(x =>
        //     {
        //         var value = x.Value<string>("key");
        //         return value == "mResourceResolver";
        //     }).FirstOrDefault()["value"] = $"Characters/{championExtract}/Skins/Skin0/Resources";
        //     var m = ParsedJson["entries"]["value"]["items"][1];
        //     m["key"] = $"Characters/{championExtract}/Skins/Skin0/Resources";
        //     File.WriteAllText(Path.Combine(Path.GetTempPath(), "KoolChanger.tmp",
        //         championExtract + ".extracted", "data",
        //         "characters",
        //         championExtract, "skins", "skin40.json"), JsonConvert.SerializeObject(ParsedJson));
        //     
        //     
        //     rt.ConvertJsonToBin(Path.Combine(Path.GetTempPath(), "KoolChanger.tmp",
        //             championExtract+ ".extracted", "data",
        //         "characters",
        //             championExtract, "skins", "skin40.json"),
        //         Path.Combine(Path.GetTempPath(), "KoolChanger.tmp", championExtract + ".complited", "data",
        //             "characters",
        //             championExtract, "skins", "skin0.bin"),
        //         Path.Combine(AppContext.BaseDirectory, "hashes")
        //         
        //         );
            
            // Console.ReadLine();
        // });
    }
}