using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
class OneClick {
static readonly string[] Supported = {
"5B64D6D540C376390470CEA3AB99D47661E8BCA81BE2BA647EA83E7D2E9428FA",
"6B6E1260CB5A1D6A303855F1C60CAB3D0BC1AEBBA7E40A964180188B271E7386"
};
public static int Main(string[] args) {
Console.OutputEncoding = new UTF8Encoding(false);
Console.WriteLine("PCL 下载限制修复工具（支持两个已适配版本）");
if(args.Length!=1){Console.Error.WriteLine("请拖入一个原版 PCL EXE；单文件版也可双击后选择文件。一次仅处理一个文件。");return 2;}
string temp=null;
try {
string input=Path.GetFullPath(args[0]);
if(!File.Exists(input) || !string.Equals(Path.GetExtension(input),".exe",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("请选择存在的 EXE 文件。");
string stem=Path.Combine(Path.GetDirectoryName(input),Path.GetFileNameWithoutExtension(input));
// Keep the input read-locked through validation and patching.
using(var source=new FileStream(input,FileMode.Open,FileAccess.Read,FileShare.Read)){
string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(source)).Replace("-","");
Console.WriteLine("输入："+input);Console.WriteLine("SHA256："+hash);
if(!Supported.Contains(hash)) {
using(var module=dnlib.DotNet.ModuleDefMD.Load(input)){
var type=module.GetTypes().FirstOrDefault(t=>t.FullName=="PCL.ModSecret");
var field=type==null?null:type.Fields.FirstOrDefault(f=>f.Name=="RestrictMcDownload");
if(field!=null && field.HasConstant && field.Constant.Value is bool && !(bool)field.Constant.Value){Console.Error.WriteLine("该文件的 RestrictMcDownload 已为 False，未重复修改；此检测不代表其他修改正确。");return 3;}
}
throw new InvalidOperationException("未适配此文件版本，已停止，未修改输入文件。请勿将本工具用于其他版本。");
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
