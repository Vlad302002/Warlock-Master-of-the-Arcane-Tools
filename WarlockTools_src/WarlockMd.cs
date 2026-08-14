using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WarlockTools
{
    /// <summary>
    /// Warlock / Warlock 2 locdata.md (mdmp). Same magic as Majesty, different tables per game.
    /// Pool is UTF-8. W1 stamp 0xC4194; W2 stamp 0x101F70.
    /// Сделал Vlad302002, истинный Арданской король и покровитель Ардании. Слава Ардании!
    /// </summary>
    static class WarlockMd
    {
        public const uint Magic = 0x706D646D;

        sealed class Layout
        {
            public uint Stamp;
            public int DefaultCodepage;
            public int Block1Cap;
            public int BucketsMod;
            public int BucketsStore;
            public int MaxSlot;
            public int OvCap;
            public int PtrBase;
            public int HashHdr;
            public int Bucket0;
            public int BucketSize;
            public int OvOff;
            public int PoolUsedOff;
            public int PoolCapOff;
            public int PoolOff;
            public int PoolCap;
            public int Payload;
            public string Name;
        }

        // Warlock 1 — Master of the Arcane
        static readonly Layout W1 = new Layout
        {
            Name = "Warlock1",
            Stamp = 0xC4194,
            DefaultCodepage = 1251,
            Block1Cap = 13313,
            BucketsMod = 16641,
            BucketsStore = 16640,
            MaxSlot = 4,
            OvCap = 64,
            PtrBase = 0x34024,
            HashHdr = 0x34020,
            Bucket0 = 0x3402C,
            BucketSize = 56,
            OvOff = 0x117864,
            PoolUsedOff = 0x117B6C,
            PoolCapOff = 0x117B70,
            PoolOff = 0x117B74,
            PoolCap = 0xC0000,
            Payload = 0x1D7B80
        };

        // Warlock 2 — The Exiled
        static readonly Layout W2 = new Layout
        {
            Name = "Warlock2",
            Stamp = 0x101F70,
            DefaultCodepage = 1251,
            Block1Cap = 12288,
            BucketsMod = 8191,
            BucketsStore = 8191,
            MaxSlot = 7,
            OvCap = 100,
            PtrBase = 0x30014,
            HashHdr = 0x30010,
            Bucket0 = 0x3001C,
            BucketSize = 92,
            OvOff = 0xE7FC0,
            PoolUsedOff = 0xE8478,
            PoolCapOff = 0xE847C,
            PoolOff = 0xE8480,
            PoolCap = 0x100000,
            // 4 + payload = 2000020 (original l100 size; 16 B after pool region)
            Payload = 0x1E8490
        };

        static Layout LayoutByStamp(uint stamp)
        {
            if (stamp == W1.Stamp) return W1;
            if (stamp == W2.Stamp) return W2;
            throw new InvalidDataException("unexpected mdmp stamp 0x" + stamp.ToString("X") +
                " (known: W1=0xC4194, W2=0x101F70)");
        }

        static int GetKey(byte[] s, int bucketsMod)
        {
            int h = 0;
            for (int i = 0; i < s.Length; i++)
            {
                sbyte c = unchecked((sbyte)s[i]);
                h = h * 31 + c;
            }
            return (int)((uint)h % (uint)bucketsMod);
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

        static byte[] Resolve(byte[] d, Layout L, int index)
        {
            int slot = index / L.BucketsMod;
            int sl;
            if (slot < L.MaxSlot)
            {
                int buck = index % L.BucketsMod;
                sl = L.Bucket0 + buck * L.BucketSize + 8 + slot * 12;
            }
            else
                sl = L.OvOff + 8 + (slot - L.MaxSlot) * 12;
            int ptr = RdI(d, sl);
            int ln = RdI(d, sl + 4);
            int fo = ptr + L.PtrBase;
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
            Layout L = LayoutByStamp(RdU(data, 4));
            int n = RdI(data, 0x0C);
            if (n < 0 || n > L.Block1Cap)
                throw new InvalidDataException("pairCount");
            var list = new List<KeyValuePair<string, string>>(n);
            var enc = new UTF8Encoding(false, false);
            for (int i = 0; i < n; i++)
            {
                int o = 0x10 + i * 16;
                string name = enc.GetString(Resolve(data, L, RdI(data, o + 4)));
                string val = enc.GetString(Resolve(data, L, RdI(data, o + 12)));
                list.Add(new KeyValuePair<string, string>(name, val));
            }
            return list;
        }

        public static void UnpackFile(string mdPath, string txtPath)
        {
            byte[] data = File.ReadAllBytes(mdPath);
            if (data.Length < 16 || RdU(data, 0) != Magic)
                throw new InvalidDataException("not mdmp");
            uint stamp = RdU(data, 4);
            Layout L = LayoutByStamp(stamp);
            int codepage = RdI(data, 8);
            var pairs = Unpack(data);
            var sb = new StringBuilder(pairs.Count * 40);
            sb.AppendFormat("{0:x8} {1:x8}\r\n", stamp, codepage);
            foreach (var p in pairs)
            {
                // Normalize CR/LF so bare \r cannot split lines on re-pack.
                string v = p.Value.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\\n");
                sb.Append(p.Key);
                sb.Append('=');
                sb.Append(v);
                sb.Append("\r\n");
            }
            File.WriteAllText(txtPath, sb.ToString(), new UTF8Encoding(false));
        }

        sealed class Table
        {
            public readonly List<int[]>[] Buckets;
            public readonly List<int[]> Overflow = new List<int[]>();
            public readonly List<byte> Pool = new List<byte> { 0 };
            public readonly Dictionary<string, int> Intern = new Dictionary<string, int>(StringComparer.Ordinal);
            public readonly int Ptr0;
            readonly Layout L;

            public Table(Layout layout)
            {
                L = layout;
                Buckets = new List<int[]>[L.BucketsStore];
                for (int i = 0; i < L.BucketsStore; i++)
                    Buckets[i] = new List<int[]>();
                Ptr0 = L.PoolOff - L.PtrBase;
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
                int buck = GetKey(s, L.BucketsMod);
                int ix = (s.Length > 0 && s[0] == (byte)'#') ? pairIdx : -1;
                var rec = new[] { ptr, ln, ix };
                int slot;
                if (buck < L.BucketsStore && Buckets[buck].Count < L.MaxSlot)
                {
                    slot = Buckets[buck].Count;
                    Buckets[buck].Add(rec);
                }
                else
                {
                    slot = L.MaxSlot + Overflow.Count;
                    Overflow.Add(rec);
                }
                enc = buck + slot * L.BucketsMod;
                Intern[key] = enc;
                return enc;
            }
        }

        static byte[] Pack(List<KeyValuePair<string, string>> pairs, Layout L, int codepage)
        {
            if (pairs.Count > L.Block1Cap)
                throw new InvalidDataException("too many pairs");
            var t = new Table(L);
            var b1 = new List<int[]>();
            var utf8 = new UTF8Encoding(false);
            for (int i = 0; i < pairs.Count; i++)
            {
                int ni = t.Add(utf8.GetBytes(pairs[i].Key), i);
                int vi = t.Add(utf8.GetBytes(pairs[i].Value), i);
                int name0 = i == 0 ? L.Block1Cap : 0;
                b1.Add(new[] { name0, ni, 0, vi });
            }
            if (t.Overflow.Count > L.OvCap)
                throw new InvalidDataException("hash overflow (>" + L.OvCap + ")");
            if (t.Pool.Count > L.PoolCap)
                throw new InvalidDataException("string pool too big");

            var buf = new byte[4 + L.Payload];
            WrU(buf, 0, Magic);
            WrU(buf, 4, L.Stamp);
            WrU(buf, 8, (uint)codepage);
            WrI(buf, 0x0C, pairs.Count);
            for (int i = 0; i < L.Block1Cap; i++)
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
            WrU(buf, L.HashHdr, 0);
            WrU(buf, L.HashHdr + 4, (uint)L.BucketsMod);
            WrU(buf, L.HashHdr + 8, (uint)L.BucketsMod);
            for (int b = 0; b < L.BucketsStore; b++)
            {
                int o = L.Bucket0 + b * L.BucketSize;
                var slots = t.Buckets[b];
                WrI(buf, o, slots.Count);
                WrI(buf, o + 4, L.MaxSlot);
                for (int s = 0; s < L.MaxSlot; s++)
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
            WrI(buf, L.OvOff, t.Overflow.Count);
            WrI(buf, L.OvOff + 4, L.OvCap);
            for (int s = 0; s < L.OvCap; s++)
            {
                int sl = L.OvOff + 8 + s * 12;
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
            WrI(buf, L.PoolUsedOff, t.Pool.Count);
            WrI(buf, L.PoolCapOff, L.PoolCap);
            t.Pool.CopyTo(buf, L.PoolOff);
            // W2 originals end with a tiny locale tag after the pool region.
            if (L.Stamp == W2.Stamp)
            {
                int fo = L.PoolOff + L.PoolCap; // 0x1E8480
                string tag = codepage == 1252 ? "en" : "ru";
                WrI(buf, fo, 2);
                WrI(buf, fo + 4, 8);
                byte[] tb = Encoding.ASCII.GetBytes(tag);
                Buffer.BlockCopy(tb, 0, buf, fo + 8, tb.Length);
            }
            return buf;
        }

        public static void PackFile(string txtPath, string mdPath)
        {
            var pairs = new List<KeyValuePair<string, string>>();
            Layout L = W1;
            int codepage = W1.DefaultCodepage;
            bool layoutSet = false;
            foreach (string raw in File.ReadAllLines(txtPath, Encoding.UTF8))
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0) continue;
                if (!layoutSet && line[0] != '#' && line.IndexOf('=') < 0)
                {
                    // header: "000c4194 000004e3" or "00101f70 000004e4"
                    string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 1)
                    {
                        uint stamp;
                        if (uint.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out stamp))
                        {
                            L = LayoutByStamp(stamp);
                            layoutSet = true;
                        }
                    }
                    if (parts.Length >= 2)
                    {
                        int cp;
                        if (int.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out cp))
                            codepage = cp;
                    }
                    continue;
                }
                if (line[0] != '#' && line.IndexOf('=') < 0) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string k = line.Substring(0, eq);
                string v = line.Substring(eq + 1).Replace("\\n", "\n");
                pairs.Add(new KeyValuePair<string, string>(k, v));
            }
            File.WriteAllBytes(mdPath, Pack(pairs, L, codepage));
        }
    }
}
