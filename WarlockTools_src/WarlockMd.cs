using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WarlockTools
{
    /// <summary>
    /// Warlock locdata.md (mdmp). Same magic as Majesty, different tables.
    /// Pool is UTF-8 (Russian), not CP1251.
    /// Сделал Vlad302002, истинный Арданской король и покровитель Ардании. Слава Ардании!
    /// </summary>
    static class WarlockMd
    {
        public const uint Magic = 0x706D646D;
        public const uint Stamp = 0xC4194;
        public const int Codepage = 1251;
        public const int Block1Cap = 13313;
        public const int BucketsMod = 16641;
        public const int BucketsStore = 16640;
        public const int MaxSlot = 4;
        public const int OvCap = 64;
        public const int PtrBase = 0x34024;
        public const int HashHdr = 0x34020;
        public const int Bucket0 = 0x3402C;
        public const int OvOff = 0x117864;
        public const int PoolUsedOff = 0x117B6C;
        public const int PoolCapOff = 0x117B70;
        public const int PoolOff = 0x117B74;
        public const int PoolCap = 0xC0000;
        public const int Payload = 0x1D7B80;

        static int GetKey(byte[] s)
        {
            int h = 0;
            for (int i = 0; i < s.Length; i++)
            {
                sbyte c = unchecked((sbyte)s[i]);
                h = h * 31 + c;
            }
            return (int)((uint)h % (uint)BucketsMod);
        }

        static int RdI(byte[] d, int o)
        {
            return BitConverter.ToInt32(d, o);
        }

        static uint RdU(byte[] d, int o)
        {
            return BitConverter.ToUInt32(d, o);
        }

        static void WrU(byte[] d, int o, uint v)
        {
            d[o] = (byte)v;
            d[o + 1] = (byte)(v >> 8);
            d[o + 2] = (byte)(v >> 16);
            d[o + 3] = (byte)(v >> 24);
        }

        static void WrI(byte[] d, int o, int v)
        {
            WrU(d, o, unchecked((uint)v));
        }

        static byte[] Resolve(byte[] d, int index)
        {
            int slot = index / BucketsMod;
            int sl;
            if (slot < MaxSlot)
            {
                int buck = index % BucketsMod;
                sl = Bucket0 + buck * 56 + 8 + slot * 12;
            }
            else
                sl = OvOff + 8 + (slot - MaxSlot) * 12;
            int ptr = RdI(d, sl);
            int ln = RdI(d, sl + 4);
            int fo = ptr + PtrBase;
            if (ln < 0 || fo < 0 || fo + ln > d.Length)
                throw new InvalidDataException("bad string ptr");
            var s = new byte[ln];
            Buffer.BlockCopy(d, fo, s, 0, ln);
            return s;
        }

        public static List<KeyValuePair<string, string>> Unpack(byte[] data)
        {
            if (data.Length < 16 || RdU(data, 0) != Magic)
                throw new InvalidDataException("not mdmp");
            if (RdU(data, 4) != Stamp)
                throw new InvalidDataException("unexpected mdmp stamp");
            int n = RdI(data, 0x0C);
            if (n < 0 || n > Block1Cap)
                throw new InvalidDataException("pairCount");
            var list = new List<KeyValuePair<string, string>>(n);
            var enc = new UTF8Encoding(false, false);
            for (int i = 0; i < n; i++)
            {
                int o = 0x10 + i * 16;
                string name = enc.GetString(Resolve(data, RdI(data, o + 4)));
                string val = enc.GetString(Resolve(data, RdI(data, o + 12)));
                list.Add(new KeyValuePair<string, string>(name, val));
            }
            return list;
        }

        public static void UnpackFile(string mdPath, string txtPath)
        {
            var pairs = Unpack(File.ReadAllBytes(mdPath));
            var sb = new StringBuilder(pairs.Count * 40);
            sb.AppendFormat("{0:x8} {1:x8}\r\n", Stamp, Codepage);
            foreach (var p in pairs)
            {
                string v = p.Value.Replace("\r\n", "\n").Replace("\n", "\\n");
                sb.Append(p.Key);
                sb.Append('=');
                sb.Append(v);
                sb.Append("\r\n");
            }
            File.WriteAllText(txtPath, sb.ToString(), new UTF8Encoding(false));
        }

        sealed class Table
        {
            public readonly List<int[]>[] Buckets = new List<int[]>[BucketsStore];
            public readonly List<int[]> Overflow = new List<int[]>();
            public readonly List<byte> Pool = new List<byte> { 0 };
            public readonly Dictionary<string, int> Intern = new Dictionary<string, int>(StringComparer.Ordinal);
            public readonly int Ptr0 = PoolOff - PtrBase;

            public Table()
            {
                for (int i = 0; i < BucketsStore; i++)
                    Buckets[i] = new List<int[]>();
            }

            public int Add(byte[] s, int pairIdx)
            {
                string key = Encoding.UTF8.GetString(s);
                int enc;
                if (Intern.TryGetValue(key, out enc))
                    return enc;
                int ptr = Ptr0 + Pool.Count;
                Pool.AddRange(s);
                Pool.Add(0);
                int ln = s.Length;
                int buck = GetKey(s);
                int ix = (s.Length > 0 && s[0] == (byte)'#') ? pairIdx : -1;
                var rec = new[] { ptr, ln, ix };
                int slot;
                if (buck < BucketsStore && Buckets[buck].Count < MaxSlot)
                {
                    slot = Buckets[buck].Count;
                    Buckets[buck].Add(rec);
                }
                else
                {
                    slot = MaxSlot + Overflow.Count;
                    Overflow.Add(rec);
                }
                enc = buck + slot * BucketsMod;
                Intern[key] = enc;
                return enc;
            }
        }

        public static byte[] Pack(List<KeyValuePair<string, string>> pairs)
        {
            if (pairs.Count > Block1Cap)
                throw new InvalidDataException("too many pairs");
            var t = new Table();
            var b1 = new List<int[]>();
            var utf8 = new UTF8Encoding(false);
            for (int i = 0; i < pairs.Count; i++)
            {
                int ni = t.Add(utf8.GetBytes(pairs[i].Key), i);
                int vi = t.Add(utf8.GetBytes(pairs[i].Value), i);
                int name0 = i == 0 ? Block1Cap : 0;
                b1.Add(new[] { name0, ni, 0, vi });
            }
            if (t.Overflow.Count > OvCap)
                throw new InvalidDataException("hash overflow (>" + OvCap + ")");
            if (t.Pool.Count > PoolCap)
                throw new InvalidDataException("string pool too big");

            var buf = new byte[4 + Payload];
            WrU(buf, 0, Magic);
            WrU(buf, 4, Stamp);
            WrU(buf, 8, (uint)Codepage);
            WrI(buf, 0x0C, pairs.Count);
            for (int i = 0; i < Block1Cap; i++)
            {
                int o = 0x10 + i * 16;
                if (i < b1.Count)
                {
                    WrI(buf, o, b1[i][0]);
                    WrI(buf, o + 4, b1[i][1]);
                    WrI(buf, o + 8, b1[i][2]);
                    WrI(buf, o + 12, b1[i][3]);
                }
                else
                {
                    WrI(buf, o, 0);
                    WrI(buf, o + 4, -1);
                    WrI(buf, o + 8, 0);
                    WrI(buf, o + 12, -1);
                }
            }
            WrU(buf, HashHdr, 0);
            WrU(buf, HashHdr + 4, (uint)BucketsMod);
            WrU(buf, HashHdr + 8, (uint)BucketsMod);
            for (int b = 0; b < BucketsStore; b++)
            {
                int o = Bucket0 + b * 56;
                var slots = t.Buckets[b];
                WrI(buf, o, slots.Count);
                WrI(buf, o + 4, MaxSlot);
                for (int s = 0; s < MaxSlot; s++)
                {
                    int sl = o + 8 + s * 12;
                    if (s < slots.Count)
                    {
                        WrI(buf, sl, slots[s][0]);
                        WrI(buf, sl + 4, slots[s][1]);
                        WrI(buf, sl + 8, slots[s][2]);
                    }
                    else
                    {
                        WrI(buf, sl, 0);
                        WrI(buf, sl + 4, 0);
                        WrI(buf, sl + 8, -1);
                    }
                }
            }
            WrI(buf, OvOff, t.Overflow.Count);
            WrI(buf, OvOff + 4, OvCap);
            for (int s = 0; s < OvCap; s++)
            {
                int sl = OvOff + 8 + s * 12;
                if (s < t.Overflow.Count)
                {
                    WrI(buf, sl, t.Overflow[s][0]);
                    WrI(buf, sl + 4, t.Overflow[s][1]);
                    WrI(buf, sl + 8, t.Overflow[s][2]);
                }
                else
                {
                    WrI(buf, sl, 0);
                    WrI(buf, sl + 4, 0);
                    WrI(buf, sl + 8, -1);
                }
            }
            WrI(buf, PoolUsedOff, t.Pool.Count);
            WrI(buf, PoolCapOff, PoolCap);
            t.Pool.CopyTo(buf, PoolOff);
            return buf;
        }

        public static void PackFile(string txtPath, string mdPath)
        {
            var pairs = new List<KeyValuePair<string, string>>();
            foreach (string raw in File.ReadAllLines(txtPath, Encoding.UTF8))
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0) continue;
                if (line[0] != '#' && line.IndexOf('=') < 0) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string k = line.Substring(0, eq);
                string v = line.Substring(eq + 1).Replace("\\n", "\n");
                pairs.Add(new KeyValuePair<string, string>(k, v));
            }
            File.WriteAllBytes(mdPath, Pack(pairs));
        }
    }
}
