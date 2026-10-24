using System;
using System.IO;
using System.Linq;
using System.Text;
class OneClick {
public static int Main(string[] args) {
Console.OutputEncoding = new UTF8Encoding(false);
Console.WriteLine("PCL 下载限制修复工具（检查 .NET 常量特征）");
if(args.Length!=1){Console.Error.WriteLine("请拖入一个原版 PCL EXE；单文件版也可双击后选择文件。一次仅处理一个文件。");return 2;}
string temp=null;
try {
string input=Path.GetFullPath(args[0]);
if(!File.Exists(input) || !string.Equals(Path.GetExtension(input),".exe",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("请选择存在的 EXE 文件。");
string stem=Path.Combine(Path.GetDirectoryName(input),Path.GetFileNameWithoutExtension(input));
// Keep the input read-locked through validation and patching.
using(var source=new FileStream(input,FileMode.Open,FileAccess.Read,FileShare.Read)){
Console.WriteLine("输入："+input);
using(var module=dnlib.DotNet.ModuleDefMD.Load(input)){
var type=module.GetTypes().FirstOrDefault(t=>t.FullName=="PCL.ModSecret");
var field=type==null?null:type.Fields.FirstOrDefault(f=>f.Name=="RestrictMcDownload");
if(field==null || !field.IsPublic || !field.IsStatic || !field.IsLiteral || field.FieldSig==null || field.FieldSig.Type.ElementType!=dnlib.DotNet.ElementType.Boolean || !field.HasConstant || !(field.Constant.Value is bool))throw new InvalidOperationException("未找到编译后的 PCL.ModSecret.RestrictMcDownload Boolean 常量，已停止，未修改输入文件。");
if(!(bool)field.Constant.Value){Console.Error.WriteLine("该文件的 RestrictMcDownload 已为 False，未重复修改；此检测不代表其他修改正确。请确认输入的是原版文件。");return 3;}
Console.WriteLine("已找到 Public Const RestrictMcDownload As Boolean = True 的编译后常量，继续处理。");
}
temp=Path.Combine(Path.GetDirectoryName(input),".pcl-repair-"+Guid.NewGuid().ToString("N")+".tmp.exe");
Console.WriteLine("正在生成并验证副本……");
Patch.Run(new[]{input,temp,"2"});
Verify.Run(new[]{input,temp});
string output=stem+".fixed.exe";int suffix=2;
while(File.Exists(output) || Directory.Exists(output))output=stem+".fixed."+(suffix++)+".exe";
File.Move(temp,output);temp=null;
Console.WriteLine();Console.WriteLine("成功："+output);Console.WriteLine("原文件未改动。静态验证通过；未实际启动界面或测试下载。");
return 0;
}
} catch(Exception e){Console.Error.WriteLine("修复失败："+e.GetType().Name+" - "+e.Message);Console.Error.WriteLine("原文件未改动；未发布验证失败的副本。");return 1;}
finally {if(temp!=null && File.Exists(temp)){try{File.Delete(temp);}catch(Exception e){Console.Error.WriteLine("临时文件清理失败："+temp+"；"+e.Message);}}}
}
}
