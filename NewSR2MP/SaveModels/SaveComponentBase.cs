namespace NewSR2MP.SaveModels
{
    public abstract class SaveComponentBase<T> where T : SaveComponentBase<T>
    {
        public SaveComponentBase(BinaryReader reader, BinaryWriter writer)
        {
            Writer = writer;
            Reader = reader;
        }
        
        public SaveComponentBase() : this(null, null) { }
        
        public BinaryWriter Writer { get; set; }
        public BinaryReader Reader { get; set; }
        
        public virtual int ComponentVersion => 0;
        
        /// <summary>
        /// The component load identifier. Please keep it to 4 characters!
        /// </summary>
        public virtual string ComponentIdentifier => null;
        
        public List<TV> ReadList<TV>()
        {
            var count = Read<int>();
            
            var list = new List<TV>();
            for (int i = 0; i < count; i++)
                list.Add(Read<TV>());

            return list;
        }
        
        public void WriteList<TV>(List<TV> list)
        {
            Write(list.Count);
            foreach (TV item in list)
                Write(item);
        }
        
        public void WriteDictionary<TK, TV>(Dictionary<TK, TV> dictionary)
        { 
            Write(dictionary.Count);
            foreach (KeyValuePair<TK, TV> kvp in dictionary)
            {
                Write(kvp.Key);
                Write(kvp.Value);
            }
        }
        public Dictionary<TK, TV> ReadDictionary<TK, TV>()
        {
            var count = Read<int>();
            Dictionary<TK, TV> dictionary = new Dictionary<TK, TV>();
            for (int i = 0; i < count; i++)
            {
                dictionary.Add(Read<TK>(), Read<TV>());
            }
            return dictionary;
        }

        public void Write<TV>(TV value) 
        {
            var typeCode = Type.GetTypeCode(typeof(TV));

            switch (typeCode)
            {
                case TypeCode.Boolean: Writer.Write((bool)(object)value); break;
                case TypeCode.Byte: Writer.Write((byte)(object)value); break;
                case TypeCode.Double: Writer.Write((double)(object)value); break;
                case TypeCode.Int16: Writer.Write((short)(object)value); break;
                case TypeCode.Int32: Writer.Write((int)(object)value); break;
                case TypeCode.Int64: Writer.Write((long)(object)value); break;
                case TypeCode.String: Writer.Write((string)(object)value); break;
                case TypeCode.Decimal: Writer.Write((decimal)(object)value); break;
                case TypeCode.UInt16: Writer.Write((ushort)(object)value); break;
                case TypeCode.UInt32: Writer.Write((uint)(object)value); break;
                case TypeCode.UInt64: Writer.Write((ulong)(object)value); break;
                case TypeCode.SByte: Writer.Write((sbyte)(object)value); break;
                case TypeCode.Single: Writer.Write((float)(object)value); break;
                case TypeCode.Char: Writer.Write((char)(object)value); break;
                case TypeCode.Object:
                    Type type = typeof(TV);
                    Type baseType = type.BaseType;
                    while (baseType != null)
                    {
                        if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(SaveComponentBase<>))
                        {
                            value.GetType().GetMethod("WriteData").Invoke(value, new object[] { Writer, false });
                            break;
                        }

                        baseType = baseType.BaseType;
                    }
                    if (baseType == null)
                    {
                        throw new InvalidOperationException($"Unsupported type: {typeof(TV)}");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported type: {typeof(TV)}");
            }
        }

        public TV Read<TV>()
        {
            TV value = default;
            var typeCode = Type.GetTypeCode(typeof(TV));

            switch (typeCode)
            {
                case TypeCode.Boolean: value = (TV)(object)Reader.ReadBoolean(); break;
                case TypeCode.Byte: value = (TV)(object)Reader.ReadByte(); break;
                case TypeCode.Double: value = (TV)(object)Reader.ReadDouble(); break;
                case TypeCode.Int16: value = (TV)(object)Reader.ReadInt16(); break;
                case TypeCode.Int32: value = (TV)(object)Reader.ReadInt32(); break;
                case TypeCode.Int64: value = (TV)(object)Reader.ReadInt64(); break;
                case TypeCode.String: value = (TV)(object)Reader.ReadString(); break;
                case TypeCode.Decimal: value = (TV)(object)Reader.ReadDecimal(); break;
                case TypeCode.UInt16: value = (TV)(object)Reader.ReadUInt16(); break;
                case TypeCode.UInt32: value = (TV)(object)Reader.ReadUInt32(); break;
                case TypeCode.UInt64: value = (TV)(object)Reader.ReadUInt64(); break;
                case TypeCode.SByte: value = (TV)(object)Reader.ReadSByte(); break;
                case TypeCode.Single: value = (TV)(object)Reader.ReadSingle(); break;
                case TypeCode.Char: value = (TV)(object)Reader.ReadChar(); break;
                case TypeCode.Object:
                    Type type = typeof(TV);
                    Type baseType = type.BaseType;
                    while (baseType != null)
                    {
                        if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(SaveComponentBase<>))
                        {
                            object obj = type.GetConstructor(new Type[] {}).Invoke(new object[] {});
                            obj.GetType().GetMethod("ReadData").Invoke(obj, new object[] {Reader, true, 0});
                            value = (TV)obj;
                            break;
                        }

                        baseType = baseType.BaseType;
                    }
                    if (baseType == null)
                    {
                        throw new InvalidOperationException($"Unsupported type: {typeof(TV)}");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported type: {typeof(TV)}");
            }

            return value;
        }
        
        public abstract void WriteComponent();
        public abstract void ReadComponent();
        
        public abstract void UpgradeComponent(T old);

        public void WriteData(BinaryWriter writer, bool writeId = true)
        {
            Writer = writer;
            if (writeId)
                Write(ComponentIdentifier);
            Write(ComponentVersion);
            
            WriteComponent();
        }
        
        /// <param name="rootRead">This is INTERNAL! Keep it at false!</param>
        /// <param name="versionFrom">This is INTERNAL! keep it at 0!</param>
        public void ReadData(BinaryReader reader, bool rootRead = false, int versionFrom = 0)
        {
            Reader = reader;
            
            // Should the identifier be read by the component base or is already read by the component before it?
            var __id = rootRead ? "" : Read<string>();
            
            var version = versionFrom == 0 ? Read<int>() : versionFrom - 1;
            if (version != ComponentVersion)
            {
                var constructor = typeof(T).GetConstructor(new[] { typeof(BinaryReader), typeof(BinaryWriter) });
                if (constructor == null)
                {
                    throw new InvalidOperationException($"Save Component {typeof(T)} must have a constructor with (BinaryReader, BinaryWriter) parameters!");
                }
                
                var old = (T)constructor.Invoke(new object[] { Reader, null });
                if (old.ComponentVersion != version - 1)
                    throw new Exception($"Cannot downgrade the component {ComponentIdentifier} from {version} to {old.ComponentVersion}!");
                old.ReadData(reader, true, version);
                UpgradeComponent(old);
            }
            if (versionFrom == 0)
                ReadComponent();
        } 

    }
}