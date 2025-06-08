using CSLOLTool.Models;
using System.Text.Json;

namespace CSLOLTool.Services;

public class ChromaService
{
    private readonly string _imageEndpoint = "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-chroma-images/";

    public async Task<List<Chroma>> LoadChromasAsync(int championId)
    {
        using var http = new HttpClient();

        var url = $"https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champions/{championId}.json";
        using var stream = await http.GetStreamAsync(url);

        using var doc = await JsonDocument.ParseAsync(stream);
        var chromas = new List<Chroma>();

        foreach (var skin in doc.RootElement.GetProperty("skins").EnumerateArray())
        {
            if (!skin.TryGetProperty("chromas", out var chromaArray))
                continue;

            foreach (var chroma in chromaArray.EnumerateArray())
            {
                var id = chroma.GetProperty("id").GetInt32(); // 266020
                var name = chroma.GetProperty("name").GetString() ?? "";
                var path = chroma.GetProperty("chromaPath").GetString() ?? "";
                var colors = chroma.GetProperty("colors")
                                   .EnumerateArray()
                                   .Select(c => c.GetString() ?? "")
                                   .ToList();

                chromas.Add(new Chroma()
                {
                    Id = id,
                    Name = name,
                    ImageUrl = $"{_imageEndpoint}{championId}/{id}",
                    Colors = colors
                });
            }
        }

        return chromas;
    }

    //public List<Chroma>? FromFolder(string path)
    //{
    //    if(File.Exists(path + "\\README.md") == false)
    //        return null;

    //    var list = new List<Chroma>();

    //    var chromaNames = GetNamesFromFolder(path);
    //    var championId = chromaNames[0].Substring(chromaNames[0].Length - 6, 3);
    //    var chromaIds = chromaNames.Select(x => x.Substring(x.Length - 6, 6)).ToList();

    //    for (int i = 0; i < chromaNames.Count(); i++)
    //    {
    //        list.Add(new Chroma()
    //        {
    //            Name = chromaNames[i],
    //            ChromaId = int.Parse(chromaIds[i]),
    //            ImageUrl = $"{_imageEndpoint}{championId}/{chromaIds[i]}"
    //        });
    //    }

    //    return list;
    //}
    //private List<string> GetNamesFromFolder(string folderPath)
    //{
    //    if (!Directory.Exists(folderPath))
    //        return new();

    //    return Directory.GetFiles(folderPath)
    //                    .Select(x => Path.GetFileName(x))
    //                    .Where(name => !string.IsNullOrEmpty(name) && name.Contains(".zip"))
    //                    .Select(x => x.Replace(".zip", ""))
    //                    .ToList();
    //}
}
