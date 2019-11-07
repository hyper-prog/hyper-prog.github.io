using System;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using System.Collections.Generic;
using System.Management;
using System.Diagnostics;

namespace BaseInfoProvider
{
    class BaseInfoProvider
    {
        int TCPPORT;
        const int BUFFERSIZE=1024;

        List<IPAddress> acceptedIPs;
        public BaseInfoProvider(int port,string acceptlist)
        {
            TCPPORT = port;
            acceptedIPs = new List<IPAddress>();

            if (acceptlist == "*")
            {
                acceptedIPs.Add(IPAddress.Any);
                log("Accept from any IP!");
            }
            else
            {
                string[] ipss;
                ipss = acceptlist.Split(new char[] { ';' });
                foreach (string ips in ipss)
                {
                    IPAddress ip;
                    try
                    {
                        ip = IPAddress.Parse(ips);
                        acceptedIPs.Add(ip);
                        log("Accept from IP:" + ip.ToString());
                    }
                    catch
                    { continue; }
                }
            }
        }

        public void log(string msg)
        {
            msg = msg.TrimEnd(new char[] { '\n', '\r' });
            System.Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd,HH:mm")+" -> "+msg);
        }

        public void Listen()
        {
            bool accept;
            string message;
            byte[] buffer; 

            TcpClient client;
            IPAddress localAddr = IPAddress.Any;
            TcpListener server = new TcpListener(localAddr, TCPPORT);
            log("Start listening...");
            server.Start();
            log(".");

            while (true)
            {

                int readed;
                client = server.AcceptTcpClient();
                log("Connect from " + client.Client.RemoteEndPoint.ToString());

                accept = false;
                try
                {
                    foreach (IPAddress aip in acceptedIPs)
                        if(aip == IPAddress.Any ||               
                           client.Client.RemoteEndPoint.ToString().Split(new char[] { ':'})[0] == aip.ToString())
                                accept = true;
                }
                catch
                { accept = false; }

                if (!accept)
                {
                    log("Unaccepted IP closing conncection...");
                    client.Close();
                    log(".");
                    continue;
                }
                
                try
                {
                    NetworkStream stream = client.GetStream();

                    buffer = new byte[BUFFERSIZE];
                    readed = stream.Read(buffer, 0, BUFFERSIZE);
                    if (readed > 0)
                    {
                        message = System.Text.Encoding.UTF8.GetString(buffer, 0, readed);
                        buffer = null;
                        log("QUERY <- " + message);

                        message = parseMessage(message);

                        log("ANSWER -> " + message);
                        buffer = new byte[BUFFERSIZE];
                        buffer = System.Text.Encoding.UTF8.GetBytes(message);
                        stream.Write(buffer, 0, buffer.Length);
                        log("Done, close the connection...");
                    }
                }
                catch (Exception e)
                {
                    log("Error occured, closed the connection.\n Error:" + e.ToString());
                }

                buffer = null;
                client.Close();
                log(".");
            }

        }

        static void Main(string[] args)
        {
            System.Console.WriteLine("BaseInfoProvider C# - by Peter Deak\n");

            if (args.Length != 1)
            {
                System.Console.WriteLine("Error: Missing argument!\n"+
                                         "Using: bip <List of accepted IPs>\n\n"+
                                         "Example 1: bip *\n"+
                                         "Example 2: bip 192.168.1.1\n"+
                                         "Example 3: bip 192.168.1.1;192.168.1.2");
                return;
            }
            System.Console.Title = "InfoProvider";
            BaseInfoProvider ip = new BaseInfoProvider(1980,args[0]);
            ip.Listen();

        }


        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        // Info miners  //////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////

        string parseMessage(string command)
        {
            if (command.StartsWith("HELP"))
            {
                return "Using: COMMAND[=argument] where COMMAND is one of HELP PROCTOTAL PROCISRUN PROCMEM PROCRESP DSIZEMB DFREEMB DFREEPERC FEXISTS FSIZE FMDATE\n";
            }

            if (command.StartsWith("DSIZEMB"))
            {
                string letter;
                string[] parts;
                parts = command.Split(new char[] { '=' });
                if (parts.Length < 2)
                    return "ERROR";
                letter = parts[1].Substring(0, 1);
                int free;
                free = diskSpace(letter,DISK_SIZE_MB);
                if (free == -1)
                    return "ERROR";
                return free.ToString() + " Mb\n";
            }
            if (command.StartsWith("DFREEMB"))
            {
                string letter;
                string[] parts;
                parts = command.Split(new char[] { '=' });
                if (parts.Length < 2)
                    return "ERROR";
                letter = parts[1].Substring(0, 1);
                int free;
                free = diskSpace(letter, DISK_FREE_MB);
                if (free == -1)
                    return "ERROR";
                return free.ToString() + " Mb\n";
            }
            if (command.StartsWith("DFREEPERC"))
            {
                string letter;
                string[] parts;
                parts = command.Split(new char[] { '=' });
                if (parts.Length < 2)
                    return "ERROR";
                letter = parts[1].Substring(0, 1);
                int free;
                free = diskSpace(letter, DISK_FREE_PERC);
                if (free == -1)
                    return "ERROR";
                return free.ToString() + " %\n";
            }
            if (command.StartsWith("PROCISRUN"))
            {
                string par;
                string[] parts;
                parts = command.Split(new char[] { '=' });
                if (parts.Length < 2)
                    return "ERROR";
                par = parts[1].TrimEnd(new char[] { '\n', '\r' });
                int result;
                result = processMiner(par,PROC_EXISTS);
                if (result < 0)
                    return "ERROR";
                if (result == 0)
                    return "NO";
                if (result > 0)
                    return "YES";
            }
            if (command.StartsWith("PROCMEM"))
            {
                string par;
                string[] parts;
                parts = command.Split(new char[] { '=' });
                if (parts.Length < 2)
                    return "ERROR";
                par = parts[1].TrimEnd(new char[] { '\n', '\r' });
                int result;
                result = processMiner(par,PROC_MEM);
                if (result < 0)
                    return "ERROR";
                return result.ToString() + "\n";
            }
            if (command.StartsWith("PROCRESP"))
            {
                string par;
                string[] parts;
                parts = command.Split(new char[] { '=' });
                if (parts.Length < 2)
                    return "ERROR";
                par = parts[1].TrimEnd(new char[] { '\n', '\r' });
                int result;
                result = processMiner(par, PROC_RESP);
                if (result < 0)
                    return "ERROR";
                if (result == 0)
                    return "NO";
                if (result > 0)
                    return "YES";
            }
            if (command.StartsWith("PROCTOTAL"))
            {
                int result;
                result = processMiner("nothing_put_here", PROC_TOTAL);
                if (result < 0)
                    return "ERROR";
                return result.ToString() + "\n";
            }

            if (command.StartsWith("FEXISTS"))
            {
                string par;
                string[] parts;
                parts = command.Split(new char[] { '=' });
                if (parts.Length < 2)
                    return "ERROR";
                par = parts[1].TrimEnd(new char[] { '\n', '\r' });
                return fileMiner(par,FILE_EXISTS);
            }
            if (command.StartsWith("FMDATE"))
            {
                string par;
                string[] parts;
                parts = command.Split(new char[] { '=' });
                if (parts.Length < 2)
                    return "ERROR";
                par = parts[1].TrimEnd(new char[] { '\n', '\r' });
                return fileMiner(par, FILE_MDATE);
            }
            if (command.StartsWith("FSIZE"))
            {
                string par;
                string[] parts;
                parts = command.Split(new char[] { '=' });
                if (parts.Length < 2)
                    return "ERROR";
                par = parts[1].TrimEnd(new char[] { '\n', '\r' });
                return fileMiner(par, FILE_SIZE);
            }
            return "UNKNOWN_COMMAND";
        }

        const int DISK_SIZE_MB = 0;
        const int DISK_FREE_MB = 1;
        const int DISK_FREE_PERC = 2;
        int diskSpace(string letter,int mode)
        {
            Int64 sizei,freei;
            string sizes,frees;
            ManagementObject disk = new ManagementObject("win32_logicaldisk.deviceid='" + letter + ":'");
            
            try
            {
                disk.Get();

                sizes = disk["Size"].ToString();
                frees = disk["FreeSpace"].ToString();
                sizei = Int64.Parse(sizes);    sizei /= (1024 * 1024);
                freei = Int64.Parse(frees);    freei /= (1024 * 1024);
            }
            catch (Exception e)
            {
                log("Error: " + e.ToString() + "\n");
                return -1;
            }

            if (mode == DISK_SIZE_MB) return (int)sizei;
            if (mode == DISK_FREE_MB) return (int)freei;
            if (mode == DISK_FREE_PERC) return (int)(((double)freei / (double)sizei) * 100);

            return -1;
        }

        const int PROC_EXISTS = 0;
        const int PROC_MEM    = 1;
        const int PROC_RESP   = 2;
        const int PROC_TOTAL  = 3;
        int processMiner(string parameter, int mode)
        {
            int num = 0;
            System.Diagnostics.Process[] processes;

            processes = System.Diagnostics.Process.GetProcesses();
            foreach(Process process in processes)
            {
                ++num;
                if (mode != PROC_TOTAL && process.ProcessName == parameter)
                {
                    if (mode == PROC_EXISTS)
                        return 1;
                    if (mode == PROC_MEM)
                    {
                        Int64 vms;
                        vms = process.PagedMemorySize64+process.NonpagedSystemMemorySize64;
                        //vms = process.VirtualMemorySize64;
                        vms /= (1024 * 1024);
                        return (int)vms;
                    }
                    if (mode == PROC_RESP)
                        return (process.Responding ? 1 : 0);
                    
                }
            }
            if (mode == PROC_TOTAL)
                return num;

            if (mode == PROC_EXISTS)
                return 0;
            else
                return -1;
        }

        const int FILE_EXISTS = 0;
        const int FILE_SIZE   = 1;
        const int FILE_MDATE  = 2;
        string fileMiner(string param, int mode)
        {
            FileInfo fi = new FileInfo(param);

            if (mode == FILE_EXISTS)
            {
                if (fi.Exists)
                    return "YES";
                else
                    return "NO";
            }
            if(!fi.Exists)
                return "ERROR";
            if (mode == FILE_SIZE)
            {
                Int64 size;
                size = fi.Length;
                size /= (1024*1024);
                return size.ToString() + " Mb";
            }
            if (mode == FILE_MDATE)
            {
                return fi.LastWriteTime.ToString();
            }
            return "ERROR";
        }
    }
}
