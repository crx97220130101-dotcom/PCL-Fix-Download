using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
class Verify {
static string Hash(byte[] b){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(b)).Replace("-","");}
static string Normal(MethodDef m){if(m.Body==null)return "none";var b=m.Body;Func<Instruction,int> pos=i=>i==null?-1:b.Instructions.IndexOf(i);var lines=b.Instructions.Select(i=>{object o=i.Operand;var op=o as Instruction;if(op!=null)o="@"+pos(op);var ops=o as IList<Instruction>;if(ops!=null)o=string.Join(",",ops.Select(x=>pos(x)));return i.OpCode.Name+" "+o;});return string.Join("\n",lines)+"\nLOCALS "+string.Join(",",b.Variables.Select(v=>v.Type.FullName))+"\nEH "+string.Join(";",b.ExceptionHandlers.Select(e=>e.HandlerType+":"+pos(e.TryStart)+":"+pos(e.TryEnd)+":"+pos(e.HandlerStart)+":"+pos(e.HandlerEnd)+":"+pos(e.FilterStart)+":"+e.CatchType));}
static void Assert(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
static void Main(string[] args){Run(args);}
public static void Run(string[] args){using(var a=ModuleDefMD.Load(args[0]))using(var b=ModuleDefMD.Load(args[1])){
var old=a.GetTypes().SelectMany(t=>t.Methods).ToDictionary(m=>m.FullName);var cur=b.GetTypes().SelectMany(t=>t.Methods).ToDictionary(m=>m.FullName);
var changed=old.Keys.Where(k=>Normal(old[k])!=Normal(cur[k])).ToArray();foreach(var x in changed)Console.WriteLine("CHANGED "+x);
Assert(changed.Length==6,"exactly six original methods changed");Assert(cur.Count==old.Count+1,"exactly one delegate adapter added");
var constantChanges=new List<string>();foreach(var t in a.GetTypes()){var u=b.GetTypes().Single(x=>x.FullName==t.FullName);foreach(var f in t.Fields){var g=u.Fields.Single(x=>x.Name==f.Name);if(f.HasConstant && !object.Equals(f.Constant.Value,g.Constant.Value))constantChanges.Add(f.FullName);}}
Assert(constantChanges.Count==1 && constantChanges[0].Contains("RestrictMcDownload"),"only requested field constant changed");
Assert((bool)b.GetTypes().Single(t=>t.FullName=="PCL.ModSecret").Fields.Single(f=>f.Name=="RestrictMcDownload").Constant.Value==false,"RestrictMcDownload is false");
Assert(a.Resources.Count==b.Resources.Count,"managed resource count preserved");foreach(var r in a.Resources.OfType<EmbeddedResource>()){var s=(EmbeddedResource)b.Resources.Single(x=>x.Name==r.Name);Assert(Hash(r.CreateReader().ToArray())==Hash(s.CreateReader().ToArray()),"resource preserved: "+r.Name);}
Assert(a.Assembly.FullName==b.Assembly.FullName,"assembly identity preserved");Assert(a.Mvid==b.Mvid,"module identity preserved");Assert(a.EntryPoint.FullName==b.EntryPoint.FullName,"entrypoint preserved");Assert(a.Cor20HeaderFlags==b.Cor20HeaderFlags,"CLR flags preserved");
var loader=cur.Values.Single(m=>m.DeclaringType.FullName=="PCL.PageDownloadInstall"&&m.Name=="LoaderInit");Assert(loader.Body.Instructions.Any(i=>i.OpCode==OpCodes.Ldsfld && ((IField)i.Operand).Name=="DlClientListLoader"),"normal Minecraft list loader selected");
var pack=cur.Values.Single(m=>m.DeclaringType.FullName=="PCL.ModModpack"&&m.Name=="ModpackInstall"&&m.MethodSig.Params.Count==3);Assert(pack.Body.Instructions.Count(i=>i.Operand is IMethod && ((IMethod)i.Operand).Name.String.StartsWith("InstallPack"))==7,"all six pack formats and compressed fallback restored");Assert(pack.Body.ExceptionHandlers.Count==old[pack.FullName].Body.ExceptionHandlers.Count,"pack exception handlers preserved");
var versions=cur.Values.Single(m=>m.DeclaringType.FullName=="PCL.PageDownloadResourceDetail"&&m.Name=="InitializeStackResourceVersions");Assert(versions.Body.Instructions.Any(i=>i.OpCode==OpCodes.Ldftn && ((IMethod)i.Operand).Name=="Install_Click"),"resource install delegate restored");
Console.WriteLine("ALL STATIC CHECKS PASSED");}}
}
