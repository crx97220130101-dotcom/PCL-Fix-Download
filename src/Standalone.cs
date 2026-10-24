using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
class Standalone {
[STAThread]
static int Main(string[] args) {
Console.OutputEncoding=new UTF8Encoding(false);
AppDomain.CurrentDomain.AssemblyResolve+=Resolve;
int result=1;
try {
if(args.Length==1 && args[0]=="--license") {Console.WriteLine(EmbeddedText("dnlib.LICENSE.txt"));return 0;}
if(args.Length==1 && args[0]=="--package-check") {
Console.WriteLine(Assembly.Load("dnlib").GetName().FullName);
Console.WriteLine("Embedded license characters: "+EmbeddedText("dnlib.LICENSE.txt").Length);
Console.WriteLine("Single-file package dependency check passed.");return 0;
}
if(args.Length==0) {
using(var picker=new OpenFileDialog()){
picker.Title="选择需要修复的原版 PCL EXE";picker.Filter="EXE 程序 (*.exe)|*.exe";picker.CheckFileExists=true;picker.Multiselect=false;
if(picker.ShowDialog()!=DialogResult.OK)return 2;
args=new[]{picker.FileName};
}
}
result=RunCore(args);
}catch(Exception e){Console.Error.WriteLine("工具运行失败："+e.GetType().Name+" - "+e.Message);}
if(Environment.GetEnvironmentVariable("PCL_REPAIR_NOPAUSE")!="1" && !Console.IsInputRedirected){Console.WriteLine();Console.WriteLine("按回车关闭窗口……");Console.ReadLine();}
return result;
}
[MethodImpl(MethodImplOptions.NoInlining)]
static int RunCore(string[] args){return OneClick.Main(args);}
static Assembly Resolve(object sender,ResolveEventArgs e){
if(new AssemblyName(e.Name).Name!="dnlib")return null;
using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("dnlib.dll")){
if(s==null)throw new InvalidOperationException("缺少内置 dnlib 依赖。");
using(var buffer=new MemoryStream()){s.CopyTo(buffer);return Assembly.Load(buffer.ToArray());}
}
}
static string EmbeddedText(string name){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)){if(s==null)throw new InvalidOperationException("Missing resource: "+name);using(var r=new StreamReader(s,Encoding.UTF8))return r.ReadToEnd();}}
}
