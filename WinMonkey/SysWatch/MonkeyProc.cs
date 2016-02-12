using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WinMonkey
{
    public class MonkeyProc : IEquatable<MonkeyProc>
    {
        public string Name { get; private set; }
        public int Id { get; private set; }

        public static readonly IEqualityComparer<MonkeyProc> ProcessComparer = new ProcComparer();

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

        public static IEnumerable<MonkeyProc> Enum()
        {
            foreach (Process p in Process.GetProcesses()) {
                yield return new MonkeyProc(p);
            }
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        private class ProcComparer : IEqualityComparer<MonkeyProc>
        {

            public bool Equals(MonkeyProc x, MonkeyProc y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(MonkeyProc obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}