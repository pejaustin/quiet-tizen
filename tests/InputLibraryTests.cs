using System;
using System.IO;

static class InputLibraryTests
{
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
 static void Main(string[] args)
 {
  var inputs=InputLibrary.Load(args[0]);
  Check(inputs.Count==4,"Powered-off devices must remain in the configured list");
  Check(inputs[0].Name=="Fire TV Stick"&&inputs[0].Action=="hdmi:15","Featured card must select HDMI 3");
  Check(InputLibrary.IndexOf(inputs,13)==2&&inputs[2].Hdmi==1,"Switch mapping must not use the upstream HDMI 3 example");
  Check(InputLibrary.IndexOf(inputs,16)==3&&inputs[3].Hdmi==4,"HDMI 4 must be selectable and refocusable");
  Check(InputLibrary.IndexOf(inputs,0)==-1,"Unknown current source must not masquerade as a device");
  foreach(var input in inputs)
  {
   var root=Path.GetDirectoryName(args[0]);
   Check(File.Exists(Path.Combine(root,input.Artwork)),"Missing card artwork");
   Check(File.Exists(Path.Combine(root,input.DockArtwork)),"Missing dock artwork");
  }
  var path=Path.GetTempFileName();
  try
  {
   foreach(var invalid in new[]{"[]", "null",
    "[{\"Name\":\"One\",\"Source\":15,\"Artwork\":\"a.svg\",\"DockArtwork\":\"b.svg\"},{\"Name\":\"Two\",\"Source\":15,\"Artwork\":\"a.svg\",\"DockArtwork\":\"b.svg\"}]",
    "[{\"Name\":\"One\",\"Source\":13,\"Artwork\":\"../a.svg\",\"DockArtwork\":\"b.svg\"},{\"Name\":\"Two\",\"Source\":14,\"Artwork\":\"a.svg\",\"DockArtwork\":\"b.svg\"}]"})
   {
    File.WriteAllText(path,invalid);bool rejected=false;
    try{InputLibrary.Load(path);}catch(InvalidDataException){rejected=true;}
    Check(rejected,"Invalid input configuration accepted");
   }
  }
  finally{File.Delete(path);}
  Check(InputLibrary.ApplyDiscovery(inputs,@"[
   {""sourceType"":15,""detectedName"":""Amazon%20Fire%20TV%20stick""},
   {""sourceType"":14,""detectedName"":""PC"",""editName"":""Steam Machine""},
   {""sourceType"":16,""detectedName"":""Nintendo Switch"",""connectionState"":false,""powerState"":2},
   {""sourceType"":0,""detectedName"":""TV""}
  ]"),"Discovery should update the configured cards");
  Check(inputs[0].Name=="Amazon Fire TV stick"&&inputs[0].Artwork=="feature-fire-tv.svg","Decode detected name and select artwork");
  Check(inputs[1].Name=="HDMI 2"&&inputs[1].Artwork=="feature-hdmi.svg","Generic PC must not retain a custom name or Steam artwork");
  Check(inputs[2].Name=="HDMI 1","Missing source must remain selectable with port label");
  Check(inputs[3].Name=="Nintendo Switch"&&inputs[3].Action=="hdmi:16"&&inputs[3].Artwork=="feature-switch.svg","Powered-off detection must follow actual port, not configured device");
  InputLibrary.ApplyDiscovery(inputs,@"[{""sourceType"":13,""detectedName"":""PlayStation 4""},{""sourceType"":16,""detectedName"":""HDMI4""}]");
  Check(inputs[2].Name=="PlayStation 4"&&inputs[2].Artwork=="feature-ps4.svg"&&inputs[2].Action=="hdmi:13","Moved PS4 must update identity without changing source action");
  Check(inputs[3].Name=="HDMI 4"&&inputs[3].Artwork=="feature-hdmi.svg","Generic detection must clear old device identity");
  foreach(var name in new[]{"", " PC ", "Game Console", "Unknown Device", "HDMI 1", "Cable Box", "%0Abad"})
  {
   InputLibrary.ApplyDiscovery(inputs,"[{\"sourceType\":13,\"detectedName\":"+System.Text.Json.JsonSerializer.Serialize(name)+"}]");
   Check(inputs[2].Name=="HDMI 1","Generic/invalid name should fall back: "+name);
  }
  InputLibrary.ApplyDiscovery(inputs,@"[null,{""sourceType"":""13""},{""sourceType"":14,""detectedName"":42},{""sourceType"":13,""detectedName"":""PS4""},{""sourceType"":13,""detectedName"":""Nintendo Switch""}]");
  Check(inputs[2].Name=="HDMI 1","Conflicting duplicate IDs must not guess the device");
  bool badJson=false;
  try{InputLibrary.ApplyDiscovery(inputs,"[{");}catch(System.Text.Json.JsonException){badJson=true;}
  Check(badJson&&inputs.Count==4,"Malformed source response must not mutate/remove physical ports");
  InputLibrary.ApplyDiscovery(inputs,null);
  Check(!InputLibrary.ApplyDiscovery(inputs,"[]"),"Unchanged fallback should not trigger UI rebuilds");
  foreach(var input in inputs)Check(input.Name=="HDMI "+input.Hdmi,"Unavailable discovery must not use configured aliases");
  var cec=new System.Collections.Generic.Dictionary<int,string>{{4,"Switch 2"},{2,"Steam Machine"},{3,"Fire TV Stick"}};
  InputLibrary.ApplyDiscovery(inputs,@"[{""sourceType"":14,""detectedName"":""PC""},{""sourceType"":16,""detectedName"":""""}]",cec);
  Check(inputs[3].Name=="Switch 2"&&inputs[3].Artwork=="feature-switch.svg","CEC identifies Switch on HDMI 4 despite missing source identity");
  Check(inputs[1].Name=="Steam Machine"&&inputs[1].Artwork=="feature-steam-machine.svg","CEC supplies specific Steam Machine identity over generic PC");
  Check(inputs[0].Name=="Fire TV Stick"&&inputs[2].Name=="HDMI 1","CEC maps by physical port, never list order");
  cec[4]=null;cec[2]="PC";
  InputLibrary.ApplyDiscovery(inputs,null,cec);
  Check(inputs[3].Name=="HDMI 4"&&inputs[1].Name=="HDMI 2","Ambiguous/generic CEC names must not preserve prior device identity");
  Console.WriteLine("Input configuration, offline-device mapping, focus and artwork checks passed.");
 }
}
