using System;
using System.Threading;

namespace LidgrenTestServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            int serverport = 12484;
            int policyport = 8843;
            int maxconnections = 4;
            const string approvalMessage = "SecretValue";

            if (args.Length > 0)
                serverport = int.Parse(args[0]);
            if (args.Length > 1)
                policyport = int.Parse(args[1]);
            if (args.Length > 2)
                maxconnections = int.Parse(args[2]);

            //Console.WriteLine("Usage: CrabBattleServer.exe");
            //Console.WriteLine("       CrabBattleServer.exe srvport");
            //Console.WriteLine("       CrabBattleServer.exe srvport polyport");
            //Console.WriteLine("       CrabBattleServer.exe srvport polyport maxplayers\n");
            //Console.WriteLine("Example: CrabBattleServer.exe " + serverport + " " + policyport + " " + maxconnections + "\n");

            //Setup the policy server first.
            //const string AllPolicy =
            //    @"<?xml version='1.0'?>" +
            //        "<cross-domain-policy>" +
            //        "	<allow-access-from domain='*' to-ports='*' />" +
            //        "</cross-domain-policy>";

            // start policy server on non root port > 1023
            //SocketPolicyServer policyServer = new SocketPolicyServer(AllPolicy, policyport);
            //policyServer.Start();

            // start game server on non root port > 1023 and max connections 20
            ServerManager.Instance.InitialiseServerManager(serverport, maxconnections, approvalMessage);

            ServerManager.Instance.StartServer();

            Console.WriteLine("\n Hit 'ESC' to stop service.");
            while (!Console.KeyAvailable || Console.ReadKey().Key != ConsoleKey.Escape)
                Thread.Sleep(500);

            //policyServer.Stop();
            ServerManager.Instance.StopServer();
        }
    }
}