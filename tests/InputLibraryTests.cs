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
  Console.WriteLine("Input configuration, offline-device mapping, focus and artwork checks passed.");
 }
}
