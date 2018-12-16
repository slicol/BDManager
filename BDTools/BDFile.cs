using SGF.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using SGF;
using SGF.Network.Core;

namespace BDTools
{
    public class BDFileHead
    {
        public readonly static byte[] MAGIC = new byte[4] { 98, 100, 115, 0 };
        public const int HEAD_SIZE = 512;
        public const int NAME_SIZE = 512 - 20;

        public byte[] magic = MAGIC;
        public uint ver = 0;
        public uint zip = 0;
        public BDFileInfo info = new BDFileInfo();


        internal bool Deserialize(NetBuffer buffer)
        {
            if (buffer.BytesAvailable >= HEAD_SIZE)
            {
                BDFileHead head = this;
                head.magic = buffer.ReadBytes(4);
                head.ver = buffer.ReadUInt();
                head.zip = buffer.ReadUInt();
                head.info.size = buffer.ReadUInt();
                head.info.hash = buffer.ReadUInt();
                head.info.name = buffer.ReadUTF();
                return true;
            }

            return false;
        }

        internal NetBuffer Serialize(NetBuffer buffer)
        {
            buffer.WriteBytes(magic);
            buffer.WriteUInt(ver);
            buffer.WriteUInt(zip);
            buffer.WriteUInt(info.size);
            buffer.WriteUInt(info.hash);
            buffer.WriteUTF8(info.name);
            return buffer;
        }
    }

    public class BDFileInfo
    {
        public uint size = 0;
        public uint hash = 0;
        public string name = "";//512 - 20
    }


    public class BDFileUtils
    {
        public const int VER = 1;

        public static BDFileInfo GetFileInfo(string fullpath)
        {
            var bytes = FileUtils.ReadFile(fullpath, BDFileHead.HEAD_SIZE);
            BDFileHead head = new BDFileHead();
            NetBuffer buffer = new NetBuffer(bytes);
            buffer.AddLength(bytes.Length);
            head.Deserialize(buffer);
            return head.info;
        }

        public static string GetNextFileName(string dir, string searchPattern)
        {
            List<string> list = FileUtils.GetFileNames(dir, SearchOption.TopDirectoryOnly, searchPattern);
            int n = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var names = list[i].Split('.');
                int m = Convert.ToInt32(names[0]);
                if (n < m) n = m;
            }
            n += 1;
            return searchPattern.Replace("*", n.ToString());
        }

        public static bool Pack(string srcpath, string dstdir = "", bool bKeepSrcFile = false)
        {
            Debuger.Log("Begin:{0} -> {1}", srcpath, dstdir);

            bool ret = false;   
            if (File.Exists(srcpath))
            {
                string filename = "";
                if (string.IsNullOrEmpty(dstdir))
                {
                    PathUtils.SplitPath(srcpath, ref dstdir, ref filename, false);
                }
                else
                {
                    filename = PathUtils.GetFileName(srcpath);
                }

                string dstpath = dstdir + "/" + GetNextFileName(dstdir, "*.bds");


                try
                {
                    using (FileStream ifs = new FileStream(srcpath, FileMode.Open, FileAccess.Read))
                    {
                        long rawsize = ifs.Length;

                        using (FileStream ofs = new FileStream(dstpath, FileMode.CreateNew, FileAccess.Write))
                        {
                            NetBuffer headbuffer = new NetBuffer(BDFileHead.HEAD_SIZE);

                            BDFileHead head = new BDFileHead();
                            head.ver = VER;
                            head.zip = 0;
                            head.info.name = filename;
                            head.info.hash = 0;
                            head.info.size = (uint)rawsize;

                            head.Serialize(headbuffer);
                            ofs.Write(headbuffer.GetBytes(), 0, BDFileHead.HEAD_SIZE);


                            byte[] buffer = new byte[1024 * 1024];
                            int chunkcnt = (int)(rawsize / buffer.Length);
                            int readsize = (int)(rawsize % buffer.Length);


                            for (int i = 0; i < chunkcnt; i++)
                            {
                                ifs.Read(buffer, 0, buffer.Length);
                                ofs.Write(buffer, 0, buffer.Length);
                            }

                            if (readsize > 0)
                            {
                                ifs.Read(buffer, 0, readsize);
                                ofs.Write(buffer, 0, readsize);
                            }

  
                        }
                    }

                    ret = true;

                    if (!bKeepSrcFile)
                    {
                        File.Delete(srcpath);
                    }

                    Debuger.Log("End");

                }
                catch (Exception e)
                {
                    Debuger.LogError("Path:{0}, Error:{1}", srcpath, e.Message);
                }
            }
            else
            {
                Debuger.LogError("File is Not Exist: {0}", srcpath);
            }
            
            return ret;
        }


        public static bool UnPack(string srcpath, string dstdir = "")
        {
            Debuger.Log("Begin:{0} -> {1}", srcpath, dstdir);

            bool ret = false;
            if (File.Exists(srcpath))
            {
                if (string.IsNullOrEmpty(dstdir))
                {
                    string filename = "";
                    PathUtils.SplitPath(srcpath, ref dstdir, ref filename, false);
                }

                BDFileInfo info = GetFileInfo(srcpath);
                string dstpath = dstdir + "/" + info.name;
                if (File.Exists(dstpath))
                {
                    Debuger.LogError("Dst File Has Existed:{0}", dstpath);
                    return false;
                }

                try
                {
                    using (FileStream ifs = new FileStream(srcpath, FileMode.Open, FileAccess.Read))
                    {
                        NetBuffer headbuffer = new NetBuffer(BDFileHead.HEAD_SIZE);
                        ifs.Read(headbuffer.GetBytes(), 0, BDFileHead.HEAD_SIZE);
                        headbuffer.AddLength(BDFileHead.HEAD_SIZE);

                        BDFileHead head = new BDFileHead();
                        head.Deserialize(headbuffer);

                        long rawsize = ifs.Length - BDFileHead.HEAD_SIZE;

                        if (head.info.size != rawsize)
                        {
                            Debuger.LogError("File Size Error, SaveFileSize:{0}, ReadFileSize:{1}", head.info.size,rawsize);
                            return false;
                        }


                        using (FileStream ofs = new FileStream(dstpath, FileMode.CreateNew, FileAccess.Write))
                        {
                            byte[] buffer = new byte[1024 * 1024];
                            int chunkcnt = (int)(rawsize / buffer.Length);
                            int readsize = (int)(rawsize % buffer.Length);


                            for (int i = 0; i < chunkcnt; i++)
                            {
                                ifs.Read(buffer, 0, buffer.Length);
                                ofs.Write(buffer, 0, buffer.Length);
                            }

                            if (readsize > 0)
                            {
                                ifs.Read(buffer, 0, readsize);
                                ofs.Write(buffer, 0, readsize);
                            }

                        }

                        ret = true;
                        Debuger.Log("End");
                    }

                }
                catch (Exception e)
                {
                    Debuger.LogError("Path:{0}, Error:{1}", srcpath, e.Message);
                }
            }
            else
            {
                Debuger.LogError("File is Not Exist: {0}", srcpath);
            }
            return ret;
        }


        public static int PackDir(string srcdir, SearchOption option, bool bKeepSrcFile = false)
        {
            var list = FileUtils.GetFileFullNames(srcdir, option, "*.*", new []{".bds",".exe",".dll",".bat",".pdb"});
            for (int i = 0; i < list.Count; i++)
            {
                Pack(list[i], "", bKeepSrcFile);
            }
            return list.Count;
        }

        public static int UnPackDir(string srcdir, SearchOption option)
        {
            var list = FileUtils.GetFileFullNames(srcdir, option, "*.bds");
            for (int i = 0; i < list.Count; i++)
            {
                UnPack(list[i]);
            }
            return list.Count;
        }

    }
}
