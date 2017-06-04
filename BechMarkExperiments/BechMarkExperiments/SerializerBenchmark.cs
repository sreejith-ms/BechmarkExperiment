using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using MessagePack;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        private List<TestObject> listOfTestObjects;

        [Setup]
        public void SetupData()
        {
            int maxObjects = 1000;
            listOfTestObjects = new List<TestObject>(maxObjects);
            Random randomNumber = new Random();
            for (int index = 0; index < maxObjects; index++)
            {                
                listOfTestObjects.Add(new TestObject {
                    A = randomNumber.Next(),
                    B = (uint)index,
                    C = (byte)index,
                    D = (ushort)randomNumber.Next(65530)
                });
            }
        }

        [Benchmark]
        public void ProtobufSerializer()
        {
            byte[] result;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, listOfTestObjects);
                result = ms.ToArray();
            }

            using (var ms = new MemoryStream(result))
            {
                Serializer.Deserialize<List<TestObject>>(ms);
            }            
        }

        [Benchmark]
        public void MessagePackCSharpSerializer()
        {
            MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            var result = MessagePackSerializer.Serialize(listOfTestObjects);
            MessagePackSerializer.Deserialize<List<TestObject>>(result);
        }

        [Benchmark]
        public void MessagePackCSharpSerializerStream()
        {
            byte[] result;
            MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            using (var ms = new MemoryStream())
            {
                MessagePackSerializer.Serialize(ms, listOfTestObjects);
                result = ms.ToArray();
            }

            using (var byteStream = new MemoryStream(result))
            {
                MessagePackSerializer.Deserialize<List<TestObject>>(byteStream);
            }
        }


        [Benchmark]
        public void MessagePackCliSerializer()
        {
            var serializer = MsgPack.Serialization.MessagePackSerializer.Get<List<TestObject>>();
            byte[] result;
            using (var ms = new MemoryStream())
            {
                serializer.Pack(ms, listOfTestObjects);
                result = ms.ToArray();
            }

            using (var byteStream = new MemoryStream(result))
            {
                serializer.Unpack(byteStream);
            }
        }

        [Benchmark]
        public void JilSerializer()
        {
            var jsonString = Jil.JSON.Serialize(listOfTestObjects);
            var result = Encoding.UTF8.GetBytes(jsonString);

            var newJsonString = Encoding.UTF8.GetString(result);
            Jil.JSON.Deserialize<List<TestObject>>(newJsonString);
        }

        [Benchmark]
        public void JsonNETSerializer()
        {
            var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(listOfTestObjects);
            var result = Encoding.UTF8.GetBytes(jsonString);

            var newJsonString = Encoding.UTF8.GetString(result);
            Newtonsoft.Json.JsonConvert.DeserializeObject<List<TestObject>>(newJsonString);
        }

        [Benchmark]
        public void CustomSerializer()
        {
            int index = 0, sizeOfObject = 11;
            byte[] resultByteArray = new byte[sizeOfObject * listOfTestObjects.Count];
            foreach(var testObject in listOfTestObjects)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(testObject.A), 0, resultByteArray, index, 4);
                index += 4;
                Buffer.BlockCopy(BitConverter.GetBytes(testObject.B), 0, resultByteArray, index, 4);
                index += 4;
                Buffer.SetByte(resultByteArray, index++, testObject.C);
                Buffer.BlockCopy(BitConverter.GetBytes(testObject.D), 0, resultByteArray, index, 2);
                index += 2;
            }

            List<TestObject> convertedResult = new List<TestObject>(resultByteArray.Length/sizeOfObject);
            for (index = 0; index < resultByteArray.Length; )
            {
                var testObject = new TestObject();
                testObject.A = BitConverter.ToInt32(resultByteArray, index);
                index += 4;
                testObject.B = BitConverter.ToUInt32(resultByteArray, index);
                index += 4;
                testObject.C = resultByteArray[index++];
                testObject.D = BitConverter.ToUInt16(resultByteArray, index);
                index += 2;
                convertedResult.Add(testObject);
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
