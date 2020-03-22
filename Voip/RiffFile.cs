using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voip
{
    public class RiffFile : RiffList
    {  
        public IEnumerable<RiffBase> Chunks
        {
            get
            {
                var origin = BaseStream.Position;  
                var reader = new System.IO.BinaryReader(BaseStream);

                while (BaseStream.Position != BaseStream.Length)
                {
                    if (BaseStream.Length - BaseStream.Position < 4) break;    
                    var fourCC = ToFourCC(reader.ReadInt32());                 
                    reader.BaseStream.Seek(-4, System.IO.SeekOrigin.Current);  
                    var item = (fourCC == "LIST") ? new RiffList(BaseStream) : new RiffChunk(BaseStream) as RiffBase;
                    if (item.Broken) break;

                    yield return item;

                    var chunk = item as RiffChunk;                             
                    if (chunk != null) chunk.SkipToEnd();                      
                }

                BaseStream.Position = origin;  
            }
        }

        public System.IO.Stream BaseStream { get; private set; }
         
        public RiffFile(System.IO.Stream output, string fourCC) : base(output, "RIFF", fourCC)
        {
            BaseStream = output;
        }
         
        public RiffFile(System.IO.Stream input) : base(input)
        {
            BaseStream = input;
        }

        public override void Close()
        {
            base.Close();
            BaseStream.Close();
        }
    }

    public class RiffList : RiffBase
    {
        public string Id { get; private set; }

        private System.IO.BinaryWriter Writer;

        public RiffList(System.IO.Stream output, string fourCC, string id) : base(output, fourCC)
        {
            Writer = new System.IO.BinaryWriter(output);
            Writer.Write(ToFourCC(id));
            this.Id = id;
        }

        public RiffList(System.IO.Stream input) : base(input)
        { 
            if (input.Length - input.Position < 4)
            {
                Broken = true;
                return;
            }

            var reader = new System.IO.BinaryReader(input);
            this.Id = ToFourCC(reader.ReadInt32());
        }

        public RiffList CreateList(string fourCC)
        {
            return new RiffList(Writer.BaseStream, "LIST", fourCC);
        }

        public RiffChunk CreateChunk(string fourCC)
        {
            return new RiffChunk(Writer.BaseStream, fourCC);
        }
    }

    public class RiffChunk : RiffBase
    {
        private System.IO.BinaryWriter Writer;
        public RiffChunk(System.IO.Stream output, string fourCC) : base(output, fourCC)
        {
            Writer = new System.IO.BinaryWriter(output);
        }

        private System.IO.BinaryReader Reader;
        public RiffChunk(System.IO.Stream input) : base(input)
        {
            Reader = new System.IO.BinaryReader(input);
        }

        public byte[] ReadBytes(int count)
        {
            return Reader.ReadBytes(count);
        }

        public byte[] ReadToEnd()
        {
            var count = ChunkSize - Reader.BaseStream.Position + DataOffset;
            var bytes = ReadBytes((int)count);
            if (count % 2 > 0) Reader.BaseStream.Seek(1, System.IO.SeekOrigin.Current);
            return bytes;
        }

        public void SkipToEnd()
        {
            var count = ChunkSize - Reader.BaseStream.Position + DataOffset;
            if (count > 0) Reader.BaseStream.Seek(count, System.IO.SeekOrigin.Current);
            if (count % 2 > 0) Reader.BaseStream.Seek(1, System.IO.SeekOrigin.Current);
        }

        public void Write(byte[] data)
        {
            Writer.BaseStream.Write(data, 0, data.Length);
        }
        public void Write(int value)
        {
            Writer.Write(value);
        }
        public void WriteByte(byte value)
        {
            Writer.Write(value);
        }
    }

    public class RiffBase : IDisposable
    { 
        public long Offset { get; private set; }
        public long SizeOffset { get; private set; }
        public long DataOffset { get; private set; }

        public uint ChunkSize { get; private set; }

        public string FourCC { get; private set; }
        internal static int ToFourCC(string fourCC)
        {
            if (fourCC.Length != 4) throw new ArgumentException("fourCC need 4 lenth", "fourCC");
            return ((int)fourCC[3]) << 24 | ((int)fourCC[2]) << 16 | ((int)fourCC[1]) << 8 | ((int)fourCC[0]);
        }
        internal static string ToFourCC(int fourCC)
        {
            var bytes = new byte[4];
            bytes[0] = (byte)(fourCC >> 0 & 0xFF);
            bytes[1] = (byte)(fourCC >> 8 & 0xFF);
            bytes[2] = (byte)(fourCC >> 16 & 0xFF);
            bytes[3] = (byte)(fourCC >> 24 & 0xFF);
            return System.Text.ASCIIEncoding.ASCII.GetString(bytes);
        }

        public RiffBase(System.IO.Stream output, string fourCC)
        {
            this.FourCC = fourCC;
            this.Offset = output.Position;

            var writer = new System.IO.BinaryWriter(output);
            writer.Write(ToFourCC(fourCC));

            this.SizeOffset = output.Position;

            uint dummy_size = 0;  
            writer.Write(dummy_size);

            this.DataOffset = output.Position;
             
            OnClose = () =>
            { 
                var position = writer.BaseStream.Position;
                ChunkSize = (uint)(position - DataOffset);
                writer.BaseStream.Position = SizeOffset;
                writer.Write(ChunkSize);
                writer.BaseStream.Position = position;
            };
        }

        public bool Broken { get; protected set; }
        public RiffBase(System.IO.Stream input)
        {
            var reader = new System.IO.BinaryReader(input);
             
            if (input.Length - input.Position < 8) { Broken = true; return; } 
            this.Offset = input.Position;
            FourCC = ToFourCC(reader.ReadInt32());
            this.SizeOffset = input.Position;
            ChunkSize = reader.ReadUInt32();
            this.DataOffset = input.Position;

            if (input.Position + ChunkSize > input.Length) Broken = true;
        }

        private Action OnClose = () => { };
        public virtual void Close() { OnClose(); }

        #region IDisposable Support
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {  
                Close();
            } 
        }

        ~RiffBase() { Dispose(false); }
        #endregion
    }
}
