using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WinMonkey
{
    public struct MonkeyProc : IEquatable<MonkeyProc>
    {
        public static readonly IEqualityComparer<MonkeyProc> ProcessComparer = new ProcComparer();

        public string Name { get; private set; }
        public int Id { get; private set; }

        public MonkeyProc(int id)
        {
            Id = id;
            Name = Process.GetProcessById(id).ProcessName;
        }

        public MonkeyProc(Process proc)
        {
            Name = proc.ProcessName;
            Id = proc.Id;
        }

        public bool Equals(MonkeyProc other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            MonkeyProc? proc = obj as MonkeyProc?;
            if (proc != null)
                return Equals(proc);
            return base.Equals(obj);
        }

        public static bool operator ==(MonkeyProc left, MonkeyProc right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MonkeyProc left, MonkeyProc right)
        {
            return !left.Equals(right);
        }

        public static IEnumerable<MonkeyProc> GetProcesses()
        {
            foreach (Process p in Process.GetProcesses()) {
                yield return new MonkeyProc(p);
            }
        }

        private class ProcComparer : IEqualityComparer<MonkeyProc>
        {
            public bool Equals(MonkeyProc x, MonkeyProc y)
            {
                return x.Equals(y);
            }

            public int GetHashCode(MonkeyProc obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}