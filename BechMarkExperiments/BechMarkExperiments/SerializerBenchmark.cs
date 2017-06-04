using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using MessagePack;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;

namespace BechMarkExperiments
{
    [MemoryDiagnoser]
    [InliningDiagnoser]
    public class SerializerBenchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);
                //Add(new InliningDiagnoser());
            }
        }

        private List<TestObject> testObject;

        [Setup]
        public void SetupData()
        {
            int maxObjects = 1000;        
            testObject = new List<TestObject>(maxObjects);
            Random randomNumber = new Random();
            for (int index = 0; index < maxObjects; index++)
            {                
                testObject.Add(new TestObject {
                    A = randomNumber.Next(),
                    B = (uint)index,
                    C = (byte)index,
                    D = (ushort)randomNumber.Next(65530)
                });
            }
        }

        [Benchmark]
        public void ProtobufSerialize()
        {
            byte[] result;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, testObject);
                result = ms.ToArray();
            }

            using (var ms = new MemoryStream(result))
            {
                Serializer.Deserialize<List<TestObject>>(ms);
            }            
        }

        [Benchmark]
        public void MessagePackSerialize()
        {
            MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            var result = MessagePackSerializer.Serialize(testObject);
            MessagePackSerializer.Deserialize<List<TestObject>>(result);
        }

        [Benchmark]
        public void MessagePackSerializeStream()
        {
            byte[] result;
            MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            using (var ms = new MemoryStream())
            {
                MessagePackSerializer.Serialize(ms, testObject);
                result = ms.ToArray();
            }

            using (var byteStream = new MemoryStream(result))
            {
                MessagePackSerializer.Deserialize<List<TestObject>>(byteStream);
            }
        }


        [Benchmark]
        public void MessagePackCliSerialize()
        {
            var serializer = MsgPack.Serialization.MessagePackSerializer.Get<List<TestObject>>();
            byte[] result;
            using (var ms = new MemoryStream())
            {
                serializer.Pack(ms, testObject);
                result = ms.ToArray();
            }

            using (var byteStream = new MemoryStream(result))
            {
                serializer.Unpack(byteStream);
            }
        }

        [Benchmark]
        public void DefaultSerialize()
        {
            byte[] result = new byte[11 * testObject.Count];
            int index = 0;
            foreach(var item in testObject)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(item.A), 0, result, index, 4);
                index += 4;
                Buffer.BlockCopy(BitConverter.GetBytes(item.B), 0, result, index, 4);
                index += 4;
                Buffer.SetByte(result, index++, item.C);
                Buffer.BlockCopy(BitConverter.GetBytes(item.D), 0, result, index, 2);
                index += 2;
            }

            List<TestObject> resultObj = new List<TestObject>(testObject.Count);
            for (index = 0; index < result.Length; )
            {
                var obj = new TestObject();
                obj.A = BitConverter.ToInt32(result, index);
                index += 4;
                obj.B = BitConverter.ToUInt32(result, index);
                index += 4;
                obj.C = result[index++];
                obj.D = BitConverter.ToUInt16(result, index);
                index += 2;
                resultObj.Add(obj);
            }
        }
    }

    [ProtoContract]
    public class TestObject
    {
        [ProtoMember(1)]
        public int A;
        [ProtoMember(2)]
        public uint B;
        [ProtoMember(3)]
        public byte C;
        [ProtoMember(4)]
        public ushort D;
    }
}
