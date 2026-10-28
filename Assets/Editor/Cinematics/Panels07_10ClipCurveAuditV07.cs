#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
internal static class Panels07_10ClipCurveAuditV07
{
 const string Key="SPELLSTRIKE_P07_10_CURVE_AUDIT_V07_ONCE";
 static Panels07_10ClipCurveAuditV07(){EditorApplication.delayCall+=Run;}
 static void Run(){if(SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;SessionState.SetBool(Key,true);
 var lines=new List<string>();
 foreach(string p in new[]{"07","08","09","10"})foreach(string role in ((p=="07")?new[]{"YoungVan"}:(p=="08")?new[]{"VanderPoel"}:new[]{"OlderVan","VanderPoel"})){
  string path="Assets/Cinematics/Storyboard3D/Panels07_10_v02/Panel"+p+"/P"+p+"_"+role+"_CinematicRefinement_Animated_v02.glb";
  var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>c.length>=7.9f && c.name.StartsWith("P"+p+"_",StringComparison.Ordinal));
  foreach(var clip in clips){var bs=AnimationUtility.GetCurveBindings(clip);int dynamicCurves=0;double maxDelta=0;
   var exemplars=new List<string>();
   foreach(var b in bs){var curve=AnimationUtility.GetEditorCurve(clip,b);if(curve==null)continue;float first=curve.Evaluate(0),a=curve.Evaluate(2),mid=curve.Evaluate(4),c=curve.Evaluate(6),last=curve.Evaluate(7.9f);double delta=Math.Max(Math.Max(Math.Abs(first-a),Math.Abs(first-mid)),Math.Max(Math.Abs(first-c),Math.Abs(first-last)));
    if(delta>0.0001){dynamicCurves++;if(exemplars.Count<9)exemplars.Add(b.path+" / "+b.propertyName+" ("+first.ToString("F4")+"->"+mid.ToString("F4")+"->"+last.ToString("F4")+")");}maxDelta=Math.Max(maxDelta,delta);
   }
   lines.Add("P"+p+" "+role+" clip="+clip.name+" length="+clip.length+" bindingCount="+bs.Length+" dynamicCurves="+dynamicCurves+" maxSampledDelta="+maxDelta);
   lines.AddRange(exemplars);
  }
 }
 string dest="Library/Panel07_10MotionAudit/CurveAuditV07.txt";Directory.CreateDirectory(Path.GetDirectoryName(dest));File.WriteAllLines(dest,lines);Debug.Log("[P07-10 CURVE AUDIT] "+dest);
 }
}
#endif

