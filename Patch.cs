using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
class Patch {
static ModuleDefMD mod;
static IEnumerable<MethodDef> Methods {get{return mod.GetTypes().SelectMany(t=>t.Methods);}}
static MethodDef Method(string type,string name){return mod.GetTypes().Single(t=>t.FullName==type).Methods.Single(m=>m.Name==name);}
static IMethod FindCall(string name){return Methods.Where(m=>m.Body!=null).SelectMany(m=>m.Body.Instructions).Select(i=>i.Operand as IMethod).First(m=>m!=null && m.Name==name);}
static Instruction I(OpCode op){return Instruction.Create(op);}
static Instruction C(IMethod m){return Instruction.Create(OpCodes.Call,m);}
static void Add(CilBody b,params Instruction[] ins){foreach(var i in ins)b.Instructions.Add(i);}
static void Main(string[] args){try{Run(args);}catch(Exception e){Console.Error.WriteLine(e.GetType().FullName);Console.Error.WriteLine(e.Message);Console.Error.WriteLine(e.StackTrace);Environment.Exit(1);}}
public static void Run(string[] args){
mod=ModuleDefMD.Load(args[0]);
var secret=mod.GetTypes().Single(t=>t.FullName=="PCL.ModSecret").Fields.Single(f=>f.Name=="RestrictMcDownload");
if(!(bool)secret.Constant.Value)throw new Exception("Constant already false");
secret.Constant=new ConstantUser(false);
// Replace the closed-download task construction with the existing vanilla list loader.
var init=Method("PCL.PageDownloadInstall","LoaderInit");var ins=init.Body.Instructions;
int start=ins.ToList().FindIndex(i=>i.OpCode==OpCodes.Ldstr && (string)i.Operand=="关闭下载");
int end=ins.Skip(start).ToList().FindIndex(i=>i.OpCode==OpCodes.Newobj && ((IMethod)i.Operand).DeclaringType.FullName.StartsWith("PCL.ModLoader/LoaderTask`2"))+start;
if(start<0 || end<start)throw new Exception("Loader anchor missing");
var field=mod.GetTypes().Single(t=>t.FullName=="PCL.ModDownload").Fields.Single(f=>f.Name=="DlClientListLoader");
ins[start].OpCode=OpCodes.Ldsfld;ins[start].Operand=field;
for(int k=start+1;k<=end;k++){ins[k].OpCode=OpCodes.Nop;ins[k].Operand=null;}
Console.WriteLine("Patched LoaderInit");
// Restore the visibility expression for the Minecraft modification card.
var modify=Method("PCL.PageDownloadModify","LoadInstanceFinished");ins=modify.Body.Instructions;
int card=ins.ToList().FindIndex(i=>i.Operand is IMethod && ((IMethod)i.Operand).Name=="get_CardMinecraft");
if(card<0 || ins[card+1].GetLdcI4Value()!=2)throw new Exception("Card visibility mismatch");
ins[card+1].OpCode=OpCodes.Ldc_I4_0;ins[card+1].Operand=null;
Console.WriteLine("Patched Minecraft card visibility");
// Retain the file-drag catch handler but bypass the constant-true special branch.
var drag=Methods.Single(m=>m.DeclaringType.FullName=="PCL.FormMain/_Closure$__25-0" && m.Name=="_Lambda$__0");ins=drag.Body.Instructions;
int msg=ins.ToList().FindIndex(i=>i.OpCode==OpCodes.Ldstr && ((string)i.Operand).StartsWith("为遵守相关法律法规"));
int cond=msg+3;
if(ins[cond].OpCode.Code!=Code.Brtrue_S)throw new Exception("Drag catch mismatch");
var target=(Instruction)ins[cond].Operand;
for(int k=msg-2;k<cond;k++){ins[k].OpCode=OpCodes.Nop;ins[k].Operand=null;}
ins[cond].OpCode=OpCodes.Br;ins[cond].Operand=target;
Console.WriteLine("Patched drag catch restriction");
// Recreate the erased pack dispatch inside the existing try/finally.
var packType=mod.GetTypes().Single(t=>t.FullName=="PCL.ModModpack");
var pack=packType.Methods.Single(m=>m.Name=="ModpackInstall" && m.MethodSig.Params.Count==3);
var body=pack.Body;ins=body.Instructions;
int deny=ins.ToList().FindIndex(i=>i.OpCode==OpCodes.Ldstr && ((string)i.Operand).StartsWith("为遵守相关法律法规"));
if(deny<0 || ins[deny+2].OpCode!=OpCodes.Throw)throw new Exception("Pack denial mismatch");
var result=new Local(pack.MethodSig.RetType);body.Variables.Add(result);
var retLoad=Instruction.Create(OpCodes.Ldloc,result);var ret=I(OpCodes.Ret);
var dispatch=new List<Instruction>();
int[] cases={0,1,2,3,4,9};string[] names={"InstallPackCurseForge","InstallPackHMCL","InstallPackMMC","InstallPackMCBBS","InstallPackModrinth","InstallPackLauncherPack"};
var labels=cases.Select(n=>I(OpCodes.Nop)).ToArray();
for(int c=0;c<cases.Length;c++){dispatch.Add(Instruction.Create(OpCodes.Ldloc,body.Variables[4]));dispatch.Add(Instruction.CreateLdcI4(cases[c]));dispatch.Add(Instruction.Create(OpCodes.Beq,labels[c]));}
// Unknown layouts follow the existing compressed-pack installer.
var fallback=packType.Methods.Single(m=>m.Name=="InstallPackCompress");
dispatch.Add(I(OpCodes.Ldarg_0));dispatch.Add(Instruction.Create(OpCodes.Ldloc,body.Variables[1]));dispatch.Add(C(fallback));dispatch.Add(Instruction.Create(OpCodes.Stloc,result));dispatch.Add(Instruction.Create(OpCodes.Leave,retLoad));
for(int c=0;c<cases.Length;c++){
var fn=packType.Methods.Single(m=>m.Name==names[c]);dispatch.Add(labels[c]);dispatch.Add(I(OpCodes.Ldarg_0));dispatch.Add(Instruction.Create(OpCodes.Ldloc,body.Variables[1]));dispatch.Add(Instruction.Create(OpCodes.Ldloc,body.Variables[2]));
if(fn.MethodSig.Params.Count>=4)dispatch.Add(I(OpCodes.Ldarg_1));if(fn.MethodSig.Params.Count>=5)dispatch.Add(I(OpCodes.Ldarg_2));
dispatch.Add(C(fn));dispatch.Add(Instruction.Create(OpCodes.Stloc,result));dispatch.Add(Instruction.Create(OpCodes.Leave,retLoad));}
// Keep the original target instruction object so all existing leaves remain valid.
ins[deny].OpCode=OpCodes.Nop;ins[deny].Operand=null;
ins[deny+1].OpCode=OpCodes.Nop;ins[deny+1].Operand=null;ins[deny+2].OpCode=OpCodes.Nop;ins[deny+2].Operand=null;
for(int k=0;k<dispatch.Count;k++)ins.Insert(deny+3+k,dispatch[k]);
foreach(var eh in body.ExceptionHandlers)if(eh.HandlerEnd==null)eh.HandlerEnd=retLoad;
ins.Add(retLoad);ins.Add(ret);
Console.WriteLine("Restored pack dispatch and preserved finally");
// Restore the no-argument file picker and reuse the original worker closure.
var entry=packType.Methods.Single(m=>m.Name=="ModpackInstall" && m.MethodSig.Params.Count==0);
var closure=mod.GetTypes().Single(t=>t.FullName=="PCL.ModModpack/_Closure$__0-0");
var select=mod.GetMemberRefs().Single(r=>r.Name=="SelectFile");
var selectSig=((IMethod)select).MethodSig;
var b=new CilBody();entry.Body=b;
var cl=new Local(closure.ToTypeSig());var filter=new Local(selectSig.Params[3]);var list=new Local(selectSig.RetType);b.Variables.Add(cl);b.Variables.Add(filter);b.Variables.Add(list);b.InitLocals=true;
var file=closure.Fields.Single(f=>f.Name=="$VB$Local_File");
var listType=(GenericInstSig)selectSig.RetType;
var getCount=new MemberRefUser(mod,"get_Count",MethodSig.CreateInstance(mod.CorLibTypes.Int32),new TypeSpecUser(listType));
var getItem=new MemberRefUser(mod,"get_Item",MethodSig.CreateInstance(new GenericVar(0),mod.CorLibTypes.Int32),new TypeSpecUser(listType));
var done=I(OpCodes.Ret);
Add(b,Instruction.Create(OpCodes.Newobj,closure.FindDefaultConstructor()),Instruction.Create(OpCodes.Stloc,cl),Instruction.Create(OpCodes.Ldstr,"选择整合包压缩文件"),I(OpCodes.Ldc_I4_0),I(OpCodes.Ldnull),Instruction.Create(OpCodes.Ldloc,filter),C(select),Instruction.Create(OpCodes.Stloc,list),Instruction.Create(OpCodes.Ldloc,list),Instruction.Create(OpCodes.Brfalse,done),Instruction.Create(OpCodes.Ldloc,list),Instruction.Create(OpCodes.Callvirt,getCount),Instruction.Create(OpCodes.Brfalse,done),Instruction.Create(OpCodes.Ldloc,cl),Instruction.Create(OpCodes.Ldloc,list),I(OpCodes.Ldc_I4_0),Instruction.Create(OpCodes.Callvirt,getItem),Instruction.Create(OpCodes.Stfld,file));
var actionCtor=Methods.Where(m=>m.Body!=null).SelectMany(m=>m.Body.Instructions).Where(i=>i.OpCode==OpCodes.Newobj).Select(i=>i.Operand as IMethod).First(m=>m!=null && m.DeclaringType.FullName=="System.Action");
var run=FindCall("RunInNewThread");
Add(b,Instruction.Create(OpCodes.Ldloc,cl),Instruction.Create(OpCodes.Ldftn,closure.Methods.Single(m=>m.Name=="_Lambda$__0")),Instruction.Create(OpCodes.Newobj,actionCtor),Instruction.Create(OpCodes.Ldstr,"整合包安装"),I(OpCodes.Ldc_I4_2),C(run),I(OpCodes.Pop),done);
Console.WriteLine("Restored pack file picker and original background worker");
// Restore install + save delegates for modpack resource entries only.
var detail=mod.GetTypes().Single(t=>t.FullName=="PCL.PageDownloadResourceDetail");
var versions=detail.Methods.Single(m=>m.Name=="InitializeStackResourceVersions");ins=versions.Body.Instructions;
int to=ins.ToList().FindIndex(i=>i.Operand is IMethod && ((IMethod)i.Operand).Name=="ToListItem");
if(to<0 || ins[to-2].OpCode!=OpCodes.Ldnull)throw new Exception("Resource list anchor mismatch");
var save=(IMethod)ins[to-4].Operand;var clickCtor=(IMethod)ins[to-3].Operand;
var iconType=mod.GetTypes().Single(t=>t.FullName=="PCL.MyIconButton/ClickEventHandler");
var iconCtor=iconType.Methods.Single(m=>m.IsInstanceConstructor);
var bridge=new MethodDefUser("RestrictPatch_SaveModpack",MethodSig.CreateInstance(mod.CorLibTypes.Void,mod.CorLibTypes.Object,mod.CorLibTypes.Object),MethodImplAttributes.IL|MethodImplAttributes.Managed,MethodAttributes.Private|MethodAttributes.HideBySig);
detail.Methods.Add(bridge);bridge.Body=new CilBody();Add(bridge.Body,I(OpCodes.Ldarg_0),I(OpCodes.Ldarg_1),I(OpCodes.Ldarg_2),Instruction.Create(OpCodes.Castclass,new TypeRefUser(mod,"System","EventArgs",mod.CorLibTypes.AssemblyRef)),C(save),I(OpCodes.Ret));
// The icon click handler uses Object and EventArgs too; match its actual signature exactly.
var invoke=iconType.Methods.Single(m=>m.Name=="Invoke");bridge.MethodSig=MethodSig.CreateInstance(mod.CorLibTypes.Void,invoke.MethodSig.Params.ToArray());
var resField=detail.Fields.Single(f=>f.Name=="TargetResourceType");
// ResourceTypes.ModPack = 2 is verified against upstream PCLCS/Constants.cs.
var install=detail.Methods.Single(m=>m.Name=="Install_Click");
int modpackValue=int.Parse(args[2]);
var sequence=new List<Instruction>();var normal=I(OpCodes.Ldarg_0);var join=ins[to-1];
sequence.Add(I(OpCodes.Ldarg_0));sequence.Add(Instruction.Create(OpCodes.Ldfld,resField));sequence.Add(Instruction.CreateLdcI4(modpackValue));sequence.Add(Instruction.Create(OpCodes.Bne_Un,normal));
sequence.Add(I(OpCodes.Ldarg_0));sequence.Add(Instruction.Create(OpCodes.Ldftn,install));sequence.Add(Instruction.Create(OpCodes.Newobj,clickCtor));sequence.Add(I(OpCodes.Ldarg_0));sequence.Add(Instruction.Create(OpCodes.Ldftn,bridge));sequence.Add(Instruction.Create(OpCodes.Newobj,iconCtor));sequence.Add(Instruction.Create(OpCodes.Br,join));
sequence.Add(normal);sequence.Add(Instruction.Create(OpCodes.Ldftn,save));sequence.Add(Instruction.Create(OpCodes.Newobj,clickCtor));sequence.Add(I(OpCodes.Ldnull));
int replace=to-5;
for(int k=replace;k<to-1;k++){ins[k].OpCode=OpCodes.Nop;ins[k].Operand=null;}
for(int k=0;k<sequence.Count;k++)ins.Insert(replace+k,sequence[k]);
Console.WriteLine("Restored resource modpack install + save actions");
foreach(var m in new[]{init,modify,drag,pack,entry,versions,bridge}){m.Body.SimplifyBranches();m.Body.OptimizeBranches();}
var options=new ModuleWriterOptions(mod);options.MetadataOptions.Flags|=MetadataFlags.PreserveAll;
mod.Write(args[1],options);
Console.WriteLine("WROTE "+args[1]);
}
}
