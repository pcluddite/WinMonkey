using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WinMonkey
{
    public class PipeWatcher
    {
        private const string PIPENAME = "WindowMonkey";
        private Form main;

        public PipeWatcher(Form main)
        {
            this.main = main;
        }

        public void Begin()
        {
            Thread t = new Thread(new ThreadStart(Init));
            t.Start();
        }

        private void Init()
        {
            if (PipeExists(PIPENAME)) {
                using (NamedPipeClientStream client = new NamedPipeClientStream(PIPENAME)) {
                    try {
                        client.Connect(2000);
                        byte[] data = Encoding.ASCII.GetBytes("SHOW");
                        client.Write(data, 0, data.Length);
                        client.WaitForPipeDrain();
                    }
                    catch {
                    }
                }
                Environment.Exit(1);
            }
            NamedPipeServerStream server = new NamedPipeServerStream(PIPENAME);
            while (true) {
                server.WaitForConnection();
                byte[] data = new byte[255];
                int count = server.Read(data, 0, data.Length);
                string msg = Encoding.ASCII.GetString(data, 0, count).ToUpper();
                if (msg.Equals("SHOW")) {
                    main.Invoke(new MethodInvoker(delegate
                    {
                        main.Show();
                        main.WindowState = FormWindowState.Normal;
                    }));
                }
                else if (msg.Equals("BREAK")) {
                    break;
                }
                else if (msg.Equals("EXIT")) {
                    Environment.Exit(0);
                }
                server.Disconnect();
            }
        }

        private static bool PipeExists(string pipe)
        {
            foreach (string s in FileIO.FindFiles(@"\\.\pipe\*")) {
                if (s.Equals(pipe)) {
                    return true;
                }
            }
            return false;
        }
    }
}
