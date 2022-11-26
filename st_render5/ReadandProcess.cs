﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net;

/// <summary>
/// Class1 的摘要说明
/// </summary>
/// 
namespace st_render5
{
    public partial class strender
    {
        public static HSdata ReadHSDFile(string path, string file)
        {
            HSdata dt = ReadHSDFile(path, file, true);
            return dt;
        }
        public static HSdata ReadHSDFile(string path, string file,bool deleteDAT)
        {
            HSdata dt = new HSdata();
            string DAT = file.Remove(file.Length - 4);

            if (File.Exists(path + file))
            {
                if (!(File.Exists(path + DAT)))
                {
                    Funcs.depress(path + file);
                }
                FileStream rdf = new FileStream(path + DAT, FileMode.Open);
                BinaryReader br = new BinaryReader(rdf);
                //br.ReadBytes(6+16);
                //dt.SatelliteName = br.ReadBytes(16);
                //br.ReadBytes(48);
                //dt.TotalHLenth = br.ReadUInt32();
                //br.ReadBytes(40);
                //dt.FileName = br.ReadBytes(129);
                br.ReadBytes(282);
                Console.WriteLine(br.ReadByte());
                br.ReadBytes(4);
                dt.w = br.ReadUInt16();
                dt.h = br.ReadUInt16();
                br.ReadBytes(41);
                Console.WriteLine(br.ReadByte());
                br.ReadBytes(268);
                dt.band = br.ReadUInt16();
                dt.wl = br.ReadDouble();
                dt.bitnum = br.ReadUInt16();
                br.ReadBytes(4);
                dt.Slope = br.ReadDouble();
                dt.intc = br.ReadDouble();
                if (dt.band > 6)
                {
                    dt.c0 = br.ReadDouble();
                    dt.c1 = br.ReadDouble();
                    dt.c2 = br.ReadDouble();
                    br.ReadBytes(24);
                    dt.c = br.ReadDouble();
                    dt.H = br.ReadDouble();
                    dt.k = br.ReadDouble();
                    br.ReadBytes(40);
                }
                else
                {
                    br.ReadBytes(112);
                }
                Console.WriteLine(br.ReadByte());
                br.ReadBytes(47 + 258);
                Console.WriteLine(br.ReadByte());
                dt.len8 = br.ReadUInt16();
                Console.WriteLine(dt.len8);
                br.ReadBytes(dt.len8 - 2);
                dt.len9 = br.ReadUInt16();
                br.ReadBytes(dt.len9 - 2);
                dt.len10 = br.ReadUInt32();
                for (UInt32 i = 0; i < dt.len10; i++)
                {
                    br.ReadByte();
                }
                br.ReadBytes(254);
                dt.data = new UInt16[dt.w * dt.h];
                int pixn = (dt.w * dt.h);
                for (int i = 0; i < pixn; i++)
                {
                    dt.data[i] = br.ReadUInt16();
                }
                //Console.WriteLine("processing data");

                //---------read complete ----------
                br.Close();
                br.Dispose();
                rdf.Close();
                rdf.Dispose();
                if (deleteDAT)
                {
                    File.Delete(path + DAT);
                }
                dt.isread = 1;
                return dt;
            }
            dt.isread = 0;
            return dt;
        }
        public static double[] Calibration(UInt16[] data, double wavel, double c, double h, double k, double Slope, double intc)
        {
            double wl = wavel * 1e-6;
            double t = 0;
            double t1 = (h * c) / (k * wl);
            double h2cc = 2 * h * c * c;
            double wl5 = wl * wl * wl * wl * wl;
            double[] temp = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                t = (Slope * data[i]) + intc;
                t *= 1e6;
                t = Math.Log(h2cc / (wl5 * t) + 1);
                temp[i] = t1 / t;

            }
            return temp;
        }
        public static void RenderATyphoonFLDK(string band,string color)
        {
            Console.WriteLine("filename:");
            string filename;
            filename = Console.ReadLine();
            Console.WriteLine("Band (from 01 to 16):");
            band = Console.ReadLine();
            Console.WriteLine("choose one color:(BD,WVNRL,2):");
            color = Console.ReadLine();
            TYInfo ty = new TYInfo(filename);
            ty.color = color;
            while (ty.end == 0)
            {
                ty.DownloadH8FromAWS(band);
                ty.RenderH8(500);
                ty.PrintInfo();
                for (int i = 0; i < 10; i++)
                {
                    ty.Next();
                }
            }
            ty.close();
        }

    }
    public partial class TYInfo
    {
        private StreamReader r;
        //private int t;
        private int dMIN;
        private string time;
        private string timeo;
        private string dfn;

        public byte end;
        public TYInfo(string fn)
        {
            r = null;
            try
            {
                r = new StreamReader(fn);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                end = 1;
                Environment.Exit(0);
            }
            MIN = 0;
            dMIN = 0;
            end = 0;
            Message = r.ReadLine();

            Ys = "";
            Ms = "";
            Hs = "";
            Ds = "";
            lats = "";
            lons = "";
            for (int p = 0; p < 4; p++)
            {
                Ys += Message[p + 8];
            }
            for (int p = 0; p < 2; p++)
            {
                Ms += Message[p + 12];
            }
            for (int p = 0; p < 2; p++)
            {
                Ds += Message[p + 14];
            }
            for (int p = 0; p < 2; p++)
            {
                Hs += Message[p + 16];
            }
            for (int p = 0; p < 3; p++)
            {
                lats += Message[p + 35];
            }
            NS = Message[38];
            for (int p = 0; p < 4; p++)
            {
                lons += Message[p + 41];
            }
            EW = Message[45];
            Y = Funcs.ToInt(Ys);
            M = Funcs.ToInt(Ms);
            D = Funcs.ToInt(Ds);
            H = Funcs.ToInt(Hs);

            lat = Funcs.ToInt(lats) / 10;
            lon = Funcs.ToInt(lons) / 10;
            if (NS == 'S')
            {
                lat *= (-1);
            }
            if (EW == 'W')
            {
                lon = 360 - lon;
            }
            ReadNextMessage();
        }
        public void Next()
        {
            MIN+=1;
            dMIN++;
            lato += dlat;
            lono += dlon;
            if (dMIN == 360)
            {
                ReadNextMessage();
                dMIN = 0;
            }
            else
            {
                if (MIN == 60)
                {
                    H++;
                    MIN = 0;
                }
                if (H == 24)
                {
                    D++;
                    H = 0;
                }
                if (((M == 1) && (M == 3) && ((M == 5) && (M == 7))) && (((M == 8) && (M == 10)) && (M == 12)))
                {
                    if (D == 32)
                    {
                        D = 1;
                        M++;
                    }
                }
                else if (((M == 4) && (M == 6)) && ((M == 9) && (M == 11)))
                {
                    if (D == 31)
                    {
                        D = 1;
                        M++;
                    }
                }
                else
                {
                    if (D == 28)
                    {
                        D = 0;
                        M++;
                    }
                }
                if (M == 13)
                {
                    M = 0;
                    Y++;
                }
            }
        }
        public void ReadNextMessage()
        {
            lato = lat;
            lono = lon;
            lats = "";
            lons = "";
            MIN = 0;
            while ((time == timeo) && (r.EndOfStream != true))
            {
                timeo = time;
                time = "";
                Message = r.ReadLine();

                for (int p = 0; p < 10; p++)
                {
                    time += Message[p + 8];
                }
            }

            Ys = "";
            Ms = "";
            Hs = "";
            Ds = "";
            for (int p = 0; p < 4; p++)
            {
                Ys += Message[p + 8];
            }
            for (int p = 0; p < 2; p++)
            {
                Ms += Message[p + 12];
            }
            for (int p = 0; p < 2; p++)
            {
                Ds += Message[p + 14];
            }
            for (int p = 0; p < 2; p++)
            {
                Hs += Message[p + 16];
            }
            for (int p = 0; p < 3; p++)
            {
                lats += Message[p + 35];
            }
            NS = Message[38];
            for (int p = 0; p < 4; p++)
            {
                lons += Message[p + 41];
            }
            EW = Message[45];
            Y = Funcs.ToInt(Ys);
            M = Funcs.ToInt(Ms);
            D = Funcs.ToInt(Ds);
            H = Funcs.ToInt(Hs);
            lat = Funcs.ToInt(lats) / 10;
            lon = Funcs.ToInt(lons) / 10;
            if (NS == 'S')
            {
                lat *= (-1);
            }
            if (EW == 'W')
            {
                lon = 360 - lon;
            }
            dlat = (lat - lato) / 360;
            dlon = (lon - lono) / 360;
        }
        public void close()
        {
            r.Close();
            r.Dispose();
        }
        public void DownloadH8FromAWS(string band)
        {
            string MM;
            string DD;
            string HH;
            string min;
            if (M < 10)
            {
                MM = "0" + M.ToString();
            }
            else
            {
                MM = M.ToString();
            }
            if (D < 10)
            {
                DD = "0" + D.ToString();
            }
            else
            {
                DD = D.ToString();
            }
            if (H < 10)
            {
                HH = "0"+ H.ToString();
            }
            else
            {
                HH = H.ToString();
            }

            if (MIN < 10)
            {
                min = "0" + MIN.ToString();
            }
            else
            {
                min = MIN.ToString();
            }
            if (Y < 2020)
            {
                dfn = "HS_H08_" + Y.ToString() + MM + DD + "_" + HH + min + "_B"+band+"_FLDK_R20_S0101.DAT.bz2";
                string url = "https://noaa-himawari8.s3.amazonaws.com/AHI-L1b-FLDK/" + Y.ToString() + "/" + MM + "/" + DD + "/" + HH + min + "/";
                //if (!File.Exists(dfn + ".png"))
                {
                    if (!File.Exists(dfn))
                    {

                        Console.WriteLine(url + dfn);
                        Funcs.download(url + dfn, dfn);
                    }
                }
            }
        }
        public void RenderH8(int r)
        {
            double t1 = Math.Sin(((lon - 140) * Math.PI) / 180);
            double t2 = Math.Sin((lat * Math.PI) / 180);
            double t3 = Math.Cos((lat * Math.PI) / 180);
            /*Console.WriteLine(t1);
            Console.WriteLine(t2);
            Console.WriteLine(t3);*/
            y = (int)(2750 - t2 * 2750);
            x = (int)(2750 + (t1 * t3) * 2750);
            if (!File.Exists(dfn + ".png"))
            {
                if (File.Exists(dfn))
                {
                    strender.RenderHSD(null, dfn, color, null,dfn + ".png", x - r + 1, y - r + 1, x + r, y + r,true);
                }
            }
        }
        public void PrintInfo()
        {
            Console.WriteLine("Message: "+Message);
            Console.WriteLine("lats: "+lats);
            Console.WriteLine("lons: "+lons);
            Console.WriteLine("NS: "+NS);
            Console.WriteLine("EW: "+EW);
            Console.WriteLine(Y);
            Console.WriteLine(M);
            Console.WriteLine(D);
            Console.WriteLine(H);
            Console.WriteLine(MIN);
            Console.WriteLine("lat now: "+lat.ToString());
            Console.WriteLine("lon now: "+lon.ToString());
            Console.WriteLine("x: "+x.ToString());
            Console.WriteLine("y: "+y.ToString());
            Console.WriteLine("dlat now: " + dlat.ToString());
            Console.WriteLine("dlon now: " + dlon.ToString());
            Console.WriteLine("download file name: "+dfn);
            //Console.WriteLine();
        }
    }
}
