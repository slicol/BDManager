using SGF.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using SGF;

namespace BDTools
{
    class StaticAssemblyResolver
    {
        public StaticAssemblyResolver()
        {

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dllName = args.Name.Contains(",")
                ? args.Name.Substring(0, args.Name.IndexOf(','))
                : args.Name.Replace(".dll", "");

            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources")) return null;

            Console.WriteLine("OnStaticAssemblyResolve:{0}", dllName);

            ResourceManager rm = new ResourceManager(
                GetType().Namespace + ".Properties.Resources", Assembly.GetExecutingAssembly());

            byte[] bytes = (byte[]) rm.GetObject(dllName);
            return Assembly.Load(bytes);
        }
    }


    class Program
    {
        static Program()
        {
            new StaticAssemblyResolver();
        }
        
        static void Main(string[] args)
        {
            string src = CmdlineUtils.GetArgValue(args, "-src");

            bool argerror = false;

            if (args.Length > 1)
            {
                if (args[0].ToLower() == "-pack")
                {
                    bool keep = CmdlineUtils.HasArg(args, "-keep");

                    if (CmdlineUtils.HasArg(args, "-dir"))
                    {
                        if (string.IsNullOrEmpty(src))
                        {
                            src = Process.GetCurrentProcess().MainModule.FileName;
                            src = PathUtils.GetParentDir(src);
                        }

                        if (CmdlineUtils.HasArg(args, "-all"))
                        {
                            BDFileUtils.PackDir(src, SearchOption.AllDirectories, keep);
                        }
                        else
                        {
                            BDFileUtils.PackDir(src, SearchOption.TopDirectoryOnly, keep);
                        }

                    }
                    else
                    {
                        string dst = CmdlineUtils.GetArgValue(args, "-dst");
                        BDFileUtils.Pack(src, dst, keep);
                    }
                }
                else if (args[0].ToLower() == "-unpack")
                {
                    if (CmdlineUtils.HasArg(args, "-dir"))
                    {
                        if (string.IsNullOrEmpty(src))
                        {
                            src = Process.GetCurrentProcess().MainModule.FileName;
                            src = PathUtils.GetParentDir(src);
                        }

                        if (CmdlineUtils.HasArg(args, "-all"))
                        {
                            BDFileUtils.UnPackDir(src, SearchOption.AllDirectories);
                        }
                        else
                        {
                            BDFileUtils.UnPackDir(src, SearchOption.TopDirectoryOnly);
                        }
                    }
                    else
                    {
                        string dst = CmdlineUtils.GetArgValue(args, "-dst");
                        BDFileUtils.UnPack(src, dst);
                    }
                }
                else
                {
                    argerror = true;
                }
            }
            else
            {
                argerror = true;
            }

            if (argerror)
            {
                Debuger.LogWarning("ArgsFormat: [-pack|-unpack] [-dir [-all]] [-keep] -src srcpath -dst dstdir");
            }

            Console.ReadKey(true);
        }


    }
}
