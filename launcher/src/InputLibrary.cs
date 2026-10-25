using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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
