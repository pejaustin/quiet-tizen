using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

public sealed class HomeInput
{
 public string Name {get;set;}
 public int Source {get;set;}
 public string Artwork {get;set;}
 public string DockArtwork {get;set;}
 public int Hdmi => Source-12;
 public string Action => "hdmi:"+Source;
}

public static class InputLibrary
{
 // Only detectedName is authoritative here; editName can be a manual alias.
 public static bool ApplyDiscovery(IReadOnlyList<HomeInput> inputs,string json,IReadOnlyDictionary<int,string> cecNames=null)
 {
  var names=new Dictionary<int,string>();
  if(!string.IsNullOrWhiteSpace(json))
   using(var document=JsonDocument.Parse(json))
   {
    if(document.RootElement.ValueKind!=JsonValueKind.Array)throw new InvalidDataException("Expected a native source array.");
    var seen=new HashSet<int>();
    foreach(var source in document.RootElement.EnumerateArray())
    {
     JsonElement id,name;
     int port;
     if(source.ValueKind!=JsonValueKind.Object||!source.TryGetProperty("sourceType",out id)
        ||id.ValueKind!=JsonValueKind.Number||!id.TryGetInt32(out port)||port<13||port>16)continue;
     if(!seen.Add(port)){names[port]=null;continue;}
     names[port]=source.TryGetProperty("detectedName",out name)&&name.ValueKind==JsonValueKind.String
      ?DeviceName(name.GetString()):null;
    }
   }
  bool changed=false;
  foreach(var input in inputs)
  {
   string detected;
   names.TryGetValue(input.Source,out detected);
   string cecName;
   if(cecNames!=null&&cecNames.TryGetValue(input.Hdmi,out cecName)&&cecName!=null)
    detected=DeviceName(cecName)??detected;
   string name=detected??("HDMI "+input.Hdmi),artwork=DeviceArtwork(detected);
   if(input.Name!=name||input.Artwork!=artwork)changed=true;
   input.Name=name;input.Artwork=artwork;
  }
  return changed;
 }
 static string DeviceName(string value)
 {
  value=Uri.UnescapeDataString(value);
  if(value.Length==0||value.Length>100)return null;
  foreach(char c in value)if(char.IsControl(c))return null;
  value=Regex.Replace(value,@"\s+"," ").Trim();
  if(value.Length==0)return null;
  if(Regex.IsMatch(value,@"^(HDMI(?:[ -]?\d+)?|PC|DVI(?: PC)?|Computer|Unknown(?: Device)?|External Device|Game Console|Gaming Console|Console|Cable(?: Box)?|Satellite(?: Box)?|Set[ -]Top Box|STB|Blu[ -]?ray(?: Player)?|DVD(?: Player)?|AV(?: Receiver)?|Receiver|Home Theater|OTT|Media Player|Streaming Device|None)$",RegexOptions.IgnoreCase))return null;
  return value;
 }
 static string DeviceArtwork(string name)
 {
  if(name==null)return "feature-hdmi.svg";
  string key=name.ToLowerInvariant();
  if(key.Contains("fire tv")||key.Contains("firetv"))return "feature-fire-tv.svg";
  if(key.Contains("steam machine"))return "feature-steam-machine.svg";
  if(key.Contains("nintendo switch")||key=="switch"||key=="switch 2")return "feature-switch.svg";
  if(key=="ps4"||key.Contains("playstation 4"))return "feature-ps4.svg";
  if(key=="ps5"||key.Contains("playstation 5"))return "feature-ps5.svg";
  return "feature-hdmi.svg";
 }
 public static List<HomeInput> Load(string path)
 {
  var inputs=JsonSerializer.Deserialize<List<HomeInput>>(File.ReadAllText(path));
  if(inputs==null||inputs.Count<2||inputs.Count>4)throw new InvalidDataException("Expected two to four configured inputs.");
  var sources=new HashSet<int>();
  foreach(var input in inputs)
   if(string.IsNullOrWhiteSpace(input.Name)||input.Source<13||input.Source>16||!sources.Add(input.Source)
      ||!SafeAsset(input.Artwork)||!SafeAsset(input.DockArtwork))
    throw new InvalidDataException("Invalid input name, source or artwork.");
  return inputs;
 }
 static bool SafeAsset(string value)=>!string.IsNullOrWhiteSpace(value)&&!Path.IsPathRooted(value)&&!value.Contains("..")&&value.EndsWith(".svg",StringComparison.Ordinal);
 public static int IndexOf(IReadOnlyList<HomeInput> inputs,int source)
 {
  for(int i=0;i<inputs.Count;i++)if(inputs[i].Source==source)return i;
  return -1;
 }
}
