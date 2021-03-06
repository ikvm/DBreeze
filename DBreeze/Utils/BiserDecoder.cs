﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DBreeze.Utils
{
    public static partial class Biser
    {
        /// <summary>
        /// Biser.Decoder
        /// </summary>
        public class Decoder
        {
            internal byte[] encoded = null;

            /// <summary>
            /// Quantity of bytes needed to form latest varint
            /// </summary>
            internal byte qb = 0;

            //MemoryStream ms = null;
            internal int encPos = -1;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="encoded"></param>
            public Decoder(byte[] encoded)
            {

                this.encoded = encoded;
                if (encoded == null || encoded.Length == 0)
                    return;

            }


            ulong GetDigit() //Gets next digit from the byte[] stream, this function works only from root decoder
            {
                int shift = 0;
                ulong result = 0;
                ulong byteValue = 0;
                qb = 0;

                while (true)
                {

                    if (coldeepcnt > 0 && !coldeep[coldeepcnt - 1].collectionIsFinished && coldeep[coldeepcnt - 1].collectionShiftToPass < coldeep[coldeepcnt - 1].collectionShift)
                    {
                        byteValue = coldeep[coldeepcnt - 1].collectionBuffer[coldeep[coldeepcnt - 1].collectionShiftToPass++];
                    }
                    else
                    {
                        encPos++;
                        byteValue = (ulong)encoded[encPos]; //Automatically will throw exception in case if index is out of range   
                    }

                    result |= (byteValue & 0x7f) << shift;
                    qb++;
                    if ((byteValue & 0x80) != 0x80)
                        return result;

                    shift += 7;
                }
            }



            byte[] Read(int length)
            {
                this.encPos++;
                byte[] bt = new byte[length];
                Buffer.BlockCopy(this.encoded, this.encPos, bt, 0, length);
                this.encPos += length - 1;
                return bt;
            }

            /// <summary>
            /// Is used for checking next collection on null, before getting one of the itterators.
            /// </summary>
            /// <returns>true if null</returns>
            public bool CheckNull()
            {
                return !(this.GetDigit() == 0);
            }

            /// <summary>
            /// Universal, but a bit slower (because of the yield returns) than those ones with IDictionary or IList
            /// </summary>
            /// <param name="isNullChecked"></param>
            /// <returns></returns>
            public IEnumerable<Decoder> GetCollection(bool isNullChecked = false)
            {

                ulong prot = 0;
                if (!isNullChecked)
                    prot = this.GetDigit();

                if (prot == 0)
                {

                    int collectionLength = (int)this.GetDigit();

                    if (collectionLength > 0) //JS not noted change
                    {
                        coldeepcnt++;

                        int cp = this.encPos;

                        ch cdi = null;
                        if (coldeep.Count < coldeepcnt)
                        {
                            cdi = new ch();
                            coldeep.Add(cdi);
                        }
                        else
                            cdi = coldeep[coldeepcnt - 1];

                        if (this.qb > 1)
                        {
                            cdi.collectionShiftToPass = 0;
                            cdi.collectionShift = this.qb - 1;
                            this.encPos = cp + collectionLength - cdi.collectionShift; //JS not noted change
                            cdi.collectionBuffer = Read(cdi.collectionShift);
                            this.encPos = cp;
                        }
                        else
                        {
                            cdi.collectionShift = 0;
                            cdi.collectionShiftToPass = 0;
                        }


                        cdi.collectionIsFinished = false;

                        while (!cdi.collectionIsFinished)
                        {

                            yield return this;

                            if ((this.encPos - (cp - cdi.collectionShift)) == collectionLength)
                            {
                                cdi.collectionIsFinished = true;
                                if (cdi.collectionShift > 0)
                                    this.encPos += cdi.collectionShift;

                                coldeepcnt--;
                                break;
                            }
                        }
                    }


                }


            } //eof

            /// <summary>
            /// 
            /// </summary>
            /// <typeparam name="K"></typeparam>
            /// <param name="fk"></param>
            /// <param name="lst"></param>
            /// <param name="isNullChecked"></param>
            public void GetCollection<K>(Func<K> fk, IList<K> lst, bool isNullChecked = false)
            {
                GetCollection(fk, fk, null, lst, null, isNullChecked);
            }

            public void GetCollection<K>(Func<K> fk, ISet<K> set, bool isNullChecked = false)
            {
                GetCollection(fk, fk, null, null, set, isNullChecked);
            }

#if NET35
            public interface ISet<T> : IEnumerable<T>
            {               
                bool Add(T item);
            }
#endif

            /// <summary>
            /// 
            /// </summary>
            /// <typeparam name="K"></typeparam>
            /// <typeparam name="V"></typeparam>
            /// <param name="fk"></param>
            /// <param name="fv"></param>
            /// <param name="dict"></param>
            /// <param name="isNullChecked"></param>
            public void GetCollection<K, V>(Func<K> fk, Func<V> fv, IDictionary<K, V> dict, bool isNullChecked = false)
            {
                GetCollection(fk, fv, dict, null, null, isNullChecked);
            }

            List<ch> coldeep = new List<ch>();
            int coldeepcnt = 0;

            class ch
            {
                public int collectionShift = 0;
                public int collectionShiftToPass = 0;
                public byte[] collectionBuffer = new byte[3];
                public bool collectionIsFinished = true;
            }

            void GetCollection<K, V>(Func<K> fk, Func<V> fv, IDictionary<K, V> dict, IList<K> lst, ISet<K> set, bool isNullChecked = false)
            {

                ulong prot = 0;
                if (!isNullChecked)
                    prot = this.GetDigit();

                if (prot == 0)
                {

                    int collectionLength = (int)this.GetDigit();
                    if (collectionLength == 0) //JS not noted change
                    {
                        return;
                    }

                    coldeepcnt++;

                    int cp = this.encPos;

                    ch cdi = null;
                    if (coldeep.Count < coldeepcnt)
                    {
                        cdi = new ch();
                        coldeep.Add(cdi);
                    }
                    else
                        cdi = coldeep[coldeepcnt - 1];

                    if (this.qb > 1)
                    {
                        cdi.collectionShiftToPass = 0;
                        cdi.collectionShift = this.qb - 1;
                        this.encPos = cp + collectionLength - cdi.collectionShift; //JS not noted change
                        cdi.collectionBuffer = Read(cdi.collectionShift);
                        this.encPos = cp;
                        //collectionPos += collectionShift;
                    }
                    else
                    {
                        cdi.collectionShift = 0;
                        cdi.collectionShiftToPass = 0;
                    }


                    cdi.collectionIsFinished = false;

                    while (true)
                    {
                        if (dict == null)
                        {
                            if (lst == null)
                                set.Add(fk());
                            else
                                lst.Add(fk());
                        }
                        else
                            dict.Add(fk(), fv());


                        if ((this.encPos - (cp - cdi.collectionShift)) == collectionLength)
                        {
                            cdi.collectionIsFinished = true;
                            if (cdi.collectionShift > 0)
                                this.encPos += cdi.collectionShift;

                            coldeepcnt--;

                            break;
                        }
                    }
                }


            } //eof


            public DateTime GetDateTime()
            {
                return new DateTime(Biser.DecodeZigZag(this.GetDigit()));
            }

            public DateTime? GetDateTime_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetDateTime();
            }


            public long GetLong()
            {
                return Biser.DecodeZigZag(this.GetDigit());
            }

            public long? GetLong_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetLong();

            }

            public ulong GetULong()
            {
                return this.GetDigit();
            }

            public ulong? GetULong_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetULong();
            }

            public int GetInt()
            {
                return (int)Biser.DecodeZigZag(this.GetDigit());
            }

            public int? GetInt_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetInt();
            }

            public uint GetUInt()
            {
                return (uint)this.GetDigit();
            }

            public uint? GetUInt_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetUInt();
            }

            public short GetShort()
            {
                return (short)Biser.DecodeZigZag(this.GetDigit());
            }

            public short? GetShort_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetShort();
            }

            public ushort GetUShort()
            {
                return (ushort)Biser.DecodeZigZag(this.GetDigit());
            }

            public ushort? GetUShort_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetUShort();
            }

            public bool GetBool()
            {
                return Read(1)[0] == 1;
            }

            public bool? GetBool_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;

                return GetBool();
            }

            public sbyte GetSByte()
            {
                return (sbyte)Read(1)[0];
            }

            public sbyte? GetSByte_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetSByte();
            }

            public byte GetByte()
            {
                return Read(1)[0];
            }

            public byte? GetByte_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetByte();
            }

            public float GetFloat()
            {
                var subRet = this.GetDigit();
                var ret = BitConverter.ToSingle(BitConverter.GetBytes(subRet), 0);
                return ret;
            }

            public float? GetFloat_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetFloat();
            }

            public double GetDouble()
            {
                var subRet = this.GetDigit();
                var ret = BitConverter.ToDouble(BitConverter.GetBytes(subRet), 0);
                return ret;
            }

            public double? GetDouble_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetDouble();

            }

            public decimal GetDecimal()
            {
                int[] bits = new int[4];
                bits[0] = (int)Biser.DecodeZigZag(this.GetDigit());
                bits[1] = (int)Biser.DecodeZigZag(this.GetDigit());
                bits[2] = (int)Biser.DecodeZigZag(this.GetDigit());
                bits[3] = (int)Biser.DecodeZigZag(this.GetDigit());
                return new decimal(bits);
            }

            public decimal? GetDecimal_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetDecimal();
            }

            public char GetChar()
            {
                return GetString().ToCharArray()[0];
            }

            public char? GetChar_NULL()
            {
                if (this.GetDigit() == 1)
                    return null;
                return GetChar();

            }

            public string GetString()
            {
                var bt = GetByteArray();
                if (bt == null)
                    return null;
                if (bt.Length == 0)
                    return "";
                return bt.UTF8_GetString();
            }

            #region "Javascript Biser decoder"
            /// <summary>
            ///  Javascript Biser decoder
            /// </summary>
            /// <returns></returns>
            public long JSGetLong()
            {
                return Biser.DecodeZigZag(this.GetDigit());
            }

            /// <summary>
            /// Javascript Biser decoder
            /// </summary>
            /// <returns></returns>
            public string JSGetString()
            {
                List<long> lst = this.CheckNull() ? null : new List<long>();
                if (lst != null)
                {
                    this.GetCollection(() => { return this.JSGetLong(); }, lst, true);
                    if (lst.Count == 0)
                        return string.Empty;

                    StringBuilder sb = new StringBuilder();

                    foreach (var item in lst)
                        sb.Append((char)item);

                    return sb.ToString();
                }

                return null;
            }

            /// <summary>
            ///  Javascript Biser decoder
            /// </summary>
            /// <returns></returns>
            public double JSGetDouble()
            {
                var bt = this.Read(9);
                if (BitConverter.IsLittleEndian ^ (bt[0] == 0))
                    Array.Reverse(bt, 1, 8);

                return BitConverter.ToDouble(bt, 1);
            }

            public DateTime JSGetDate()
            {
                return (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds(this.JSGetLong());
            }

            public bool JSGetBool()
            {
                return this.Read(1)[0] == 1;
            }

            public byte[] JSGetByteArray()
            {
                byte[] ret = null;

                var prot = this.JSGetLong();
                if (prot > 0)
                    ret = Read((int)prot);

                return ret;
            }

            #endregion


            public byte[] GetByteArray()
            {
                //0 - with length, 1 - null, 2 - zero length
                byte[] ret = null;

                var prot = this.GetDigit();
                switch (prot)
                {
                    case 2:
                        ret = new byte[0];
                        break;
                    case 0:
                        ret = Read((int)((uint)this.GetDigit()));
                        break;
                }

                return ret;
            }

            public Guid GetGuid()
            {
                var res = GetByteArray();
                return new Guid(res);
            }

            public Guid? GetGuid_NULL()
            {
                var res = GetByteArray();
                if (res == null)
                    return null;
                return new Guid(res);
            }

        }//eoc Decoder

    }
}
